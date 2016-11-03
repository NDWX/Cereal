using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Pug.Cereal
{
	internal class Resource
	{
		object requestSync = new object();
		
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

		EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

		GrainComplex CreateGrain(string subject)
		{
			return new GrainComplex(new Grain(string.Format("{0} {1}/{2}", DateTime.Now.ToString("o"), Identifier, Guid.NewGuid().ToString()), subject, Identifier), this);
		}

		public void Release(Grain grain)
		{
			if (this.CurrentLock != null && this.CurrentLock.Grain.Equals(grain))
			{ // if Grain belongs to current subject, release lock and offer grain to next in queue

				CurrentLock = null;

				waitHandle.Set();
			}

			LastAccessTimestamp = DateTime.Now;
		}

		public GrainComplex RequestLock(string subject, int timeout)
		{
			GrainComplex @lock = null;

			// ensure lock-request is processed one at a time
			lock (requestSync)
			{
				if (CurrentLock != null && subject == CurrentLock.Grain.Subject)
				{
					@lock = CurrentLock;
				}
				else
				{
					if( waitHandle.WaitOne(timeout) )
					{
						@lock = CreateGrain(subject);
						CurrentLock = @lock;

						waitHandle.Reset();
					}
				}
			}

			LastAccessTimestamp = DateTime.Now;

			return @lock;
		}


		//public void Release(Grain grain)
		//{
		//	Release(grain, ReleaseReason.Released);
		//}
	}
}
