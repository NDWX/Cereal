using System;
using System.Threading;
using System.Threading.Tasks;

namespace Pug.Cereal
{
	internal class GrainComplex
	{
		Resource resource;
		object releaseSync = new object();
		CancellationTokenSource expirationNotificationToken;

		//Action<string> onFinished;

		public GrainComplex(Grain grain, Resource resource /*, Action<string> onFinished = null*/)
		{
			this.Grain = grain;
			this.resource = resource;

			expirationNotificationToken = new CancellationTokenSource();

			Task.Run(
				() => { 
					Thread.Sleep(grain.Duration - (int)Math.Ceiling(DateTime.Now.Subtract(grain.Timestamp).TotalMilliseconds));
				
					try
					{
						if (!expirationNotificationToken.Token.IsCancellationRequested)
							Return(Resource.ReleaseReason.Expired);
					}
					catch(ObjectDisposedException)
					{
					}
				},
				expirationNotificationToken.Token
			);

			//this.onFinished = onFinished;

			//ThreadPool.QueueUserWorkItem(new WaitCallback((o) => { Thread.Sleep(grain.Duration - (DateTime.Now.Subtract(grain.Timestamp))); Release(Resource.ReleaseReason.Expired); }));
		}

		public Grain Grain
		{
			get;
			protected set;
		}

		public event EventHandler<string> Returned;

		void Return(Resource.ReleaseReason reason)
		{
			lock (releaseSync)
			{
				resource.Release(Grain, reason);

				if (Returned != null)
					if (reason == Resource.ReleaseReason.Expired)
						Returned(this, Grain.Identifier);
					else
						Task.Run(() => Returned(this, Grain.Identifier));
			}
		}

		public bool HasExpired(int minimumPeriod)
		{
			return Grain.HasExpired(minimumPeriod);
		}

		public void Return()
		{
			expirationNotificationToken.Cancel();

			Return(Resource.ReleaseReason.Released);

			expirationNotificationToken.Dispose();
		}
	}
}
