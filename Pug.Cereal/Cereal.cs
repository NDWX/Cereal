using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Pug.Cereal
{
	/// <summary>
	/// Transport independent resource-lock server.
	/// </summary>
	public class Cereal : ICereal
	{
		string identifier;
		TraceSwitch traceSwitch;

		int defaultWaitTimeout;

		Dictionary<string, Grain> grains;
		Dictionary<string, Grain> resourceLocks;
		object trimSync = new object();
#if DETECT_DEADLOCK
		Dictionary<string, SubjectContext> subjectContexts;
#endif
		public Cereal(string system, int defaultWaitTimeout = 500)
		{
			this.identifier = string.Format("Pug.Cereal.{0}", system);

			traceSwitch = new TraceSwitch(string.Format("{1}", DateTime.Now.ToString("o"), system), string.Format("Pug Cereal for {0}", identifier));
			
			grains = new Dictionary<string, Grain>();

			resourceLocks = new Dictionary<string, Grain>();
#if DETECT_DEADLOCK
			subjectContexts = new Dictionary<string, SubjectContext>();
#endif
			
			this.defaultWaitTimeout = defaultWaitTimeout;
		}

		public bool HasDeadlockDetection
		{
			get
			{
#if DETECT_DEADLOCK
				return true;
#else
				return false;
#endif
			}
		}

		/// <summary>
		/// Request lock on a resource for specified duration.
		/// </summary>
		/// <param name="subject">Subject requesting the lock</param>
		/// <param name="resource">Identifier of the resource to lock</param>
		/// <param name="timeout">The maximum amount of wait time before request should be abandoned</param>
		/// <returns></returns>
		public IGrain Lock(string subject, string resource, int timeout = -1)
		{
			if (isDisposed)
				throw new ObjectDisposedException(identifier);

			Grain grain = null;
#if DETECT_DEADLOCK
			SubjectContext context = null;

			EventWaitHandle subjectWaitHandle = new EventWaitHandle(true, EventResetMode.AutoReset, string.Format("{0}/{1}", identifier, subject));

			if (!subjectWaitHandle.WaitOne(timeout))
			{
				subjectWaitHandle.Dispose();
				return null;
			}
#endif
			// find out if resource is already locked by subject
			if ( resourceLocks.TryGetValue(resource, out grain) )
			{
				if (grain.Subject == subject ) // if resource lock is currently locked by subject
				{ // return the same Grain
#if DETECT_DEADLOCK
					subjectWaitHandle.Set();
					subjectWaitHandle.Dispose();
#endif
#if TRACE
					Trace.WriteLineIf(traceSwitch.TraceWarning, string.Format("{0} {1}: Found existing lock for {2} held by subject {3}.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, resource, subject), string.Empty);
#endif
					return grain;
				}
#if DETECT_DEADLOCK
				else if( timeout < 0 ) // otherwise check for possible deadlock
				{
					// obtain context of (peer) subject currently locking resource
					SubjectContext peerContext = grain.SubjectContext;

					// if peer is waiting for another resource, check if that resource is being locked by current subject
					if ( peerContext.IsWaitingForResource )
					{
						// if subject is locking resource wanted by peer
						if (subjectContexts.TryGetValue(subject, out context) && context.HeldResources.Count > 0 && peerContext.IsWaitingFor(context.HeldResources)) 
						{
							subjectWaitHandle.Set();
							subjectWaitHandle.Dispose();
							throw new PossibleDeadlock();
						}
					}
				}
#endif
			}
#if DETECT_DEADLOCK
			// Get/register subject context if none has been acquired
			if ( context == null && !subjectContexts.TryGetValue(subject, out context))
			{
				context = new SubjectContext(subject);
				subjectContexts.Add(subject, context);
			}

			context.WaitingFor(resource);
#endif
#if TRACE
			Trace.WriteLineIf(traceSwitch.TraceVerbose, string.Format("{0} {1}: Obtaining lock for {2} on behalf of {3}.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, resource, subject), string.Empty);
#endif
			EventWaitHandle waitHandle = new EventWaitHandle(true, EventResetMode.AutoReset, string.Format("{0}/{1}", identifier, resource));

			bool lockAcquired = waitHandle.WaitOne(timeout);

			if (lockAcquired)
			{
#if TRACE
				Trace.WriteLineIf(traceSwitch.TraceVerbose, string.Format("{0} {1}: Obtained lock for {2} on behalf of {3}.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, resource, subject), string.Empty);
#endif
#if DETECT_DEADLOCK
				context.WaitSuccessful();

				grain = new Grain(string.Format("{0}:{1}/{2}", DateTime.Now.ToString("o"), resource, Guid.NewGuid().ToString()), subject, context, resource, waitHandle);
#else
				grain = new Grain(string.Format("{0}:{1}/{2}", DateTime.Now.ToString("o"), resource, Guid.NewGuid().ToString()), subject, resource, waitHandle);
#endif
				resourceLocks[resource] = grain;
				grains[grain.Identifier] = grain;
			}
			else
			{
				// if resource locked is not acquired, dispose of waitHandle
#if TRACE
				Trace.WriteLineIf(traceSwitch.TraceWarning, string.Format("{0} {1}: Failed obtaining lock for {2} on behalf of {3}, probably due to timeout.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, resource, subject), string.Empty);
#endif
				waitHandle.Dispose();
#if DETECT_DEADLOCK
				// Deregister subject context if not other resources are locked by subject
				if ( context.HeldResources.Count == 0)
					subjectContexts.Remove(subject);
				else
					context.WaitTimeout();

#endif
			}
#if DETECT_DEADLOCK
			subjectWaitHandle.Set();
			subjectWaitHandle.Dispose();
#endif
			return grain;
		}

		/// <summary>
		/// Release resource lock as specified in the <paramref name="grain"/> object.
		/// </summary>
		/// <param name="grain">Object containing information regarding the lock obtained on a resource</param>
		public void Release(IGrain grain)
		{
			if (isDisposed)
				throw new ObjectDisposedException(identifier);
#if TRACE
			Trace.WriteLineIf(traceSwitch.TraceInfo, string.Format("{0} {1}: Trying to release {2} locked by {3}.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, grain.Resource, grain.Subject), string.Empty);
#endif
			if (grains.ContainsKey(grain.Identifier))
			{
				Grain _grain = null;
				
				if(  grains.TryGetValue(grain.Identifier, out _grain) )
				{
					lock(_grain)
					{
						resourceLocks.Remove(grain.Resource);

						EventWaitHandle waitHandle = ((Grain)_grain).WaitHandle;

						waitHandle.Set();
						waitHandle.Dispose();

						grains.Remove(_grain.Identifier);
#if DETECT_DEADLOCK
						SubjectContext context = null;

						if( !subjectContexts.TryGetValue(grain.Subject, out context) )
						{
							// todo: throw exception or log error
						}

						context.Released(grain.Resource);

						EventWaitHandle subjectWaitHandle = new EventWaitHandle(true, EventResetMode.AutoReset, string.Format("{0}/{1}", identifier, grain.Subject));

						if (subjectWaitHandle.WaitOne())
						{
							if (context.HeldResources.Count == 0)
								subjectContexts.Remove(grain.Subject);

							subjectWaitHandle.Set();
							subjectWaitHandle.Dispose();
						}
						else
						{
							subjectWaitHandle.Dispose();
							// todo: throw exception
						}
#endif
					}
#if TRACE
					Trace.WriteLineIf(traceSwitch.TraceInfo, string.Format("{0} {1}: {2} released by {3}.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, grain.Resource, grain.Subject), string.Empty);
#endif
				}
				else
				{
#if TRACE
					Trace.WriteLineIf(traceSwitch.TraceInfo, string.Format("{0} {1}: Unable to find grain for {2} as specified by {3}.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, grain.Resource, grain.Subject), string.Empty);
#endif
				}
			}
#if TRACE
			else
			{
				Trace.WriteLineIf(traceSwitch.TraceWarning, string.Format("{0} {1}: {2} was not locked by {3}.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, grain.Resource, grain.Subject), string.Empty);
			}
#endif
		}
		
		/// <summary>
		/// Optimize server by trimming resource list by removing least recently locked resources.
		/// </summary>
		public void Optimize()
		{
			if (isDisposed)
				throw new ObjectDisposedException(identifier);

			lock (trimSync)
			{
			}
		}

#region IDisposable Support

		private bool isDisposed = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!isDisposed)
			{
				if (disposing)
				{
					foreach( Grain grain in resourceLocks.Values )
					{
						grain.WaitHandle.Dispose();
					}
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				isDisposed = true;

				grains.Clear();
				subjectContexts.Clear();
				resourceLocks.Clear();
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~Cereal() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}

#endregion
	}
}
