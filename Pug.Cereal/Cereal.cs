using System;
using System.Collections.Generic;
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
		
		Dictionary<string, IGrain> grains;

		int defaultWaitTimeout = 500;

		object trimSync = new object();
		Dictionary<string, IGrain> resourceLocks;

		public Cereal(string system, int defaultWaitTimeout)
		{
			this.identifier = system;

			traceSwitch = new TraceSwitch(string.Format("Pug.Cereal.{1}", DateTime.Now.ToString("o"), system), string.Format("Pug Cereal for {0}", system));
			
			grains = new Dictionary<string, IGrain>();
			resourceLocks = new Dictionary<string, IGrain>();
			
			this.defaultWaitTimeout = defaultWaitTimeout;
		}

		/// <summary>
		/// Request lock on a resource for specified duration.
		/// </summary>
		/// <param name="subject">Subject requesting the lock</param>
		/// <param name="resource">Identifier of the resource to lock</param>
		/// <param name="timeout">The maximum amount of wait time before request should be abandoned</param>
		/// <returns></returns>
		public IGrain Lock(string subject, string resource, int timeout = 0)
		{
			IGrain grain = null;

			if ( resourceLocks.ContainsKey(resource) && resourceLocks.TryGetValue(resource, out grain) && grain.Subject == subject)
			{
#if TRACE
				Trace.WriteLineIf(traceSwitch.TraceWarning, string.Format("{0} {1}: Found existing lock for {2} held by subject {3}.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, resource, subject), string.Empty);
#endif
				return grain;
			}			
#if TRACE
			Trace.WriteLineIf(traceSwitch.TraceVerbose, string.Format("{0} {1}: Obtaining lock for {2} on behalf of {3}.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, resource, subject), string.Empty);
#endif
			EventWaitHandle waitHandle = new EventWaitHandle(true, EventResetMode.AutoReset, string.Format("{0}/{1}", identifier, resource));

			if (timeout == 0)
				timeout = defaultWaitTimeout;

			bool lockAcquired = waitHandle.WaitOne(timeout);

			if (lockAcquired)
			{
#if TRACE
				Trace.WriteLineIf(traceSwitch.TraceVerbose, string.Format("{0} {1}: Obtained lock for {2} on behalf of {3}.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, resource, subject), string.Empty);
#endif
				grain = new Grain(string.Format("{0}:{1}/{2}", DateTime.Now.ToString("o"), resource, Guid.NewGuid().ToString()), subject, resource, waitHandle);
				grains[grain.Identifier] = grain;
				resourceLocks[resource] = grain;
			}
			else
			{
#if TRACE
				Trace.WriteLineIf(traceSwitch.TraceWarning, string.Format("{0} {1}: Failed obtaining lock for {2} on behalf of {3}, probably due to timeout.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, resource, subject), string.Empty);
#endif
				waitHandle.Dispose();

				grain = Grain.Empty;
			}

			return grain;
		}

		/// <summary>
		/// Release resource lock as specified in the <paramref name="grain"/> object.
		/// </summary>
		/// <param name="grain">Object containing information regarding the lock obtained on a resource</param>
		public void Release(IGrain grain)
		{
#if TRACE
			Trace.WriteLineIf(traceSwitch.TraceInfo, string.Format("{0} {1}: Trying to release {2} locked by {3}.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, grain.Resource, grain.Subject), string.Empty);
#endif

			if (grains.ContainsKey(grain.Identifier))
			{
				IGrain _grain = null;
				
				if(  grains.TryGetValue(grain.Identifier, out _grain) )
				{
					resourceLocks.Remove(grain.Resource);
					grains.Remove(_grain.Identifier);

					EventWaitHandle waitHandle = ((Grain)_grain).WaitHandle;

					waitHandle.Set();
					waitHandle.Dispose();
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
			lock (trimSync)
			{
			}
		}
	}
}
