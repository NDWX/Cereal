using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Pug.Cereal
{
	internal class Resource
	{
		public enum ReleaseReason : byte
		{
			Expired,
			Released
		}

		public struct LockRequest
		{
			public LockRequest(string subject, int duration, LockWait wait)
			{
				this.Subject = subject;
				this.Duration = duration;
				this.Wait = wait;
			}

			public LockWait Wait
			{
				get;
				private set;
			}

			public string Subject
			{
				get;
				private set;
			}

			public int Duration
			{
				get;
				private set;
			}
		}

		public class LockWait : IDisposable
		{
			ManualResetEvent wait;
			DateTime createTimestamp;

			GrainComplex grain = null;

			TraceSwitch traceSwitch;

			public LockWait(string system)
			{
				traceSwitch = new TraceSwitch(string.Format("Pug.Cereal.{1}.LockWait", DateTime.Now.ToString("o"), system), string.Format("Pug Cereal LockWait for {1}", DateTime.Now.ToString("o"), system));

				this.wait = new ManualResetEvent(false);
				this.createTimestamp = DateTime.Now;
				Expired = false;
			}

			public bool Expired
			{
				get;
				private set;
			}

			object shakeSync = new object();

			GrainComplex Shake(GrainComplex grain)
			{
				lock(shakeSync)
				{
					if( grain == null) // if this function is called by lock-request
					{
						if (this.grain != null) // if lock was offered, assign to return variable
						{
							Trace.WriteLineIf(traceSwitch.TraceVerbose, string.Format("{0} {1}: {2} accepting grain for {3}.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, this.grain.Grain.Subject, this.grain.Grain.Resource), "LockWait");

							grain = this.grain;

							Trace.WriteLineIf(traceSwitch.TraceInfo, string.Format("{0} {1}: {2} accepted grain for {3}.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, grain.Grain.Subject, grain.Grain.Resource), "LockWait");
						}
						else // if lock-wait timed-out
						{
							// mark wait as expired
							Expired = true;

							Trace.WriteLineIf(traceSwitch.TraceInfo, string.Format("{0} {1}: received no grain offer.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId), "LockWait");
						}
					}
					else // if this function is called by resource-release
					{
						if (!Expired)
						{ // if resource-lock wait has not expired

							// offer grain
							this.grain = grain;

							// signal offer
							try
							{
								wait.Set();

								Trace.WriteLineIf(traceSwitch.TraceVerbose, string.Format("{0} {1}: Grain for {2} offered to {3}.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, grain.Grain.Resource, grain.Grain.Subject), "LockWait");
							}
							catch (ObjectDisposedException objectDisposed)
							{
								Trace.WriteLineIf(traceSwitch.TraceWarning, string.Format("{0} {1}: Grain wait by {3} for {2} didn't timeout, but no longer waiting!!!!!!.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, grain.Grain.Resource, grain.Grain.Subject), "LockWait");
								// todo : log anomaly

							}
							catch (Exception error)
							{
								Trace.WriteLineIf(traceSwitch.TraceWarning, string.Format("{0} {1}: Unexpected error: {4} occured offering grain for {2} to {3}.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, grain.Grain.Resource, grain.Grain.Subject, error.Message), "LockWait");
							}

							grain = null;
						}
						else
						{
							Trace.WriteLineIf(traceSwitch.TraceInfo, string.Format("{0} {1}: Grain wait by {3} for {2} timed-out.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, grain.Grain.Resource, grain.Grain.Subject), "LockWait");
						}
					}

				}

				return grain;
			}

			GrainComplex TryAccept()
			{
				GrainComplex grain = Shake(null);

				return grain;
			}

			public GrainComplex Wait(int timeout)
			{
				Trace.WriteLineIf(traceSwitch.TraceInfo, string.Format("{0} {1}: waiting for offer.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId), "LockWait");

				// wait for lock offer signal or until timeout
				bool lockOffered = wait.WaitOne(timeout);
				
				Trace.WriteLineIf(traceSwitch.TraceVerbose && !lockOffered, string.Format("{0} {1}: Wait for grain offer timed out, attempting to accept offer.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId), "LockWait");
				
				// try accepting lock offer
				return TryAccept();
			}

			internal bool Offer(GrainComplex grain)
			{
				//bool offered = false;

				Trace.WriteLineIf(traceSwitch.TraceInfo, string.Format("{0} {1}: Offering grain for {2} to {3}.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, grain.Grain.Resource, grain.Grain.Subject), "LockWait");

				return Shake(grain) == null;

				//Shake(
				//	() =>
				//	{
				//		if (!Expired)
				//		{
				//			this.grain = grain;
				//			offered = true;

				//			try
				//			{
				//				wait.Set();

				//				Trace.WriteLineIf(traceSwitch.TraceVerbose, string.Format("{0} {1}: Grain for {2} offered to {3}.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, grain.Grain.Resource, grain.Grain.Subject), "LockWait");
				//			}
				//			catch (ObjectDisposedException objectDisposed)
				//			{
				//				Trace.WriteLineIf(traceSwitch.TraceWarning, string.Format("{0} {1}: Grain wait by {3} for {2} didn't timeout, but no longer waiting!!!!!!.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, grain.Grain.Resource, grain.Grain.Subject), "LockWait");
				//				// todo : log anomaly

				//				offered = false;
				//			}
				//			catch (Exception error)
				//			{
				//				Trace.WriteLineIf(traceSwitch.TraceWarning, string.Format("{0} {1}: Unexpected error: {4} occured offering grain for {2} to {3}.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, grain.Grain.Resource, grain.Grain.Subject, error.Message), "LockWait");
				//			}
				//		}
				//		else
				//		{
				//			Trace.WriteLineIf(traceSwitch.TraceInfo, string.Format("{0} {1}: Grain wait by {3} for {2} timed-out.", DateTime.Now.ToString("o"), Thread.CurrentThread.ManagedThreadId, grain.Grain.Resource, grain.Grain.Subject), "LockWait");
				//		}
				//	}
				//);

				//return offered;
			}

			public void Dispose()
			{
				wait.Dispose();
			}
		}

		object requestSync = new object();
		object shakeSync = new object();

		Queue<LockRequest> requests;

		protected GrainComplex CurrentLock
		{
			get;
			set;
		}

		public DateTime LastAccessTimestamp
		{
			get;
			private set;
		}

		public Resource(string identifier, Mutex syncLock)
		{
			this.Identifier = identifier;
			this.SyncLock = syncLock;

			requests = new Queue<LockRequest>();

			LastAccessTimestamp = DateTime.Now;
		}

		public string Identifier
		{
			get;
			private set;
		}

		public Mutex SyncLock
		{
			get;
			private set;
		}

		GrainComplex CreateGrain(string subject, int duration)
		{
			return new GrainComplex(new Grain(string.Format("{0} {1}/{2}", DateTime.Now.ToString("o"), Identifier, Guid.NewGuid().ToString()), subject, Identifier, duration), this);
		}

		bool CreateAndOfferGrain(LockRequest request)
		{
			// create grain to offer
			GrainComplex grain = CreateGrain(request.Subject, request.Duration);

			// offer grain to lock-request
			if (request.Wait.Offer(grain))
			{
				CurrentLock = grain;

				return true;
			}
			else
			{
				return false;
			}
		}

		void ProcessNextRequest()
		{
			if (requests.Count > 0)
			{
				LockRequest request = requests.Dequeue();
				
				// try offering lock to next lock-request
				while (!CreateAndOfferGrain(request) && requests.Count > 0)
				{
					// if lock was not accepted and there are more requests, offer to next lock-request
					request = requests.Dequeue();
				}
			}
		}

		public void Release(Grain grain, ReleaseReason reason)
		{
			if (this.CurrentLock != null && this.CurrentLock.Grain.Equals(grain))
			{ // if Grain belongs to current subject, release lock and offer grain to next in queue

				CurrentLock = null;

				// Create and offer new Grain to next thread in queue
				if (reason == ReleaseReason.Expired)
					ProcessNextRequest();
				else
					Task.Run(() => ProcessNextRequest());
			}

			LastAccessTimestamp = DateTime.Now;
		}

		public void RequestLock(string subject, int requestedDuration, LockWait wait)
		{
			// ensure lock-request is processed one at a time
			lock (requestSync)
			{
				if (CurrentLock != null && subject == CurrentLock.Grain.Subject)
				{ // if request comes from the same subject, offer the same Grain

					wait.Offer(this.CurrentLock);
				}
				else // otherwise create a new lock-request
				{
					LockRequest request = new LockRequest(subject, requestedDuration, wait);

					if (CurrentLock == null || CurrentLock.HasExpired(0))
					{ // if resource is not currently locked, create and offer Grain to self

						GrainComplex grain = CreateGrain(request.Subject, request.Duration);
						request.Wait.Offer(grain);
						CurrentLock = grain;
					}
					else
					{ // otherwise queue lock-request

						requests.Enqueue(request);
					}
				}
			}

			LastAccessTimestamp = DateTime.Now;
		}


		//public void Release(Grain grain)
		//{
		//	Release(grain, ReleaseReason.Released);
		//}
	}
}
