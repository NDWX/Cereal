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
		Dictionary<string, Resource> resources;
		Dictionary<string, GrainComplex> grains;

		string lockNameTemplate;

		int defaultWaitTimeout = 500;

		object trimSync = new object();
		Dictionary<string, DateTime> accessList;

		public Cereal(string system, int defaultWaitTimeout)
		{
			this.identifier = system;
			accessList = new Dictionary<string, DateTime>();
			resources = new Dictionary<string, Resource>();
			grains = new Dictionary<string, GrainComplex>();

			lockNameTemplate = "Pug.Cereal:" + system + ".{0}";
			this.defaultWaitTimeout = defaultWaitTimeout;
		}

		Mutex getResourceLock(string identifier)
		{
			return new Mutex(false, string.Format(lockNameTemplate, identifier));
		}

		private void onGrainReturned(object sender, string identifier)
		{
			// remove grain from index when lock is released or expired
			try
			{
				grains.Remove(identifier);
			}
			finally
			{

			}
		}

		/// <summary>
		/// Request lock on a resource for specified duration.
		/// </summary>
		/// <param name="subject">Subject requesting the lock</param>
		/// <param name="resource">Identifier of the resource to lock</param>
		/// <param name="desiredDuration">Maximum duration for which the resource will be locked</param>
		/// <param name="timeout">The maximum amount of wait time before request should be abandoned</param>
		/// <returns></returns>
		public Grain Lock(string subject, string resource, int desiredDuration, int timeout = 0)
		{
			accessList[resource] = DateTime.Now;

			Grain grain = Grain.Empty;
			Resource _resource = null;

			// obtain reference to a lock-resource internal lock
			Mutex resourceLock = getResourceLock(resource);

			if (timeout == 0)
				timeout = defaultWaitTimeout;

			// obtain internal lock on a lock-resource to ensure it is not deleted while being locked
			bool lockAcquired = resourceLock.WaitOne(defaultWaitTimeout);

			if( lockAcquired )
			{
				// retrieve reference to lock-resource from index if exists
				if( resources.ContainsKey(resource) )
				{
					_resource = resources[resource];

					resourceLock.ReleaseMutex();
					resourceLock.Dispose();
				}
				else // create lock-resource
				{
					_resource = new Resource(resource, resourceLock);
					resources[resource] = _resource;

					resourceLock.ReleaseMutex();
				}

				// create wait object, which is point of interaction between lock-request and lock-release
				Resource.LockWait wait = new Resource.LockWait(this.identifier);
				_resource.RequestLock(subject, desiredDuration, wait);

				// wait for lock to be granted
				GrainComplex _lock = wait.Wait(timeout);

				wait.Dispose();

				// subscribe to lock-release and index resource-lock
				if (_lock != null)
				{
					_lock.Returned += onGrainReturned;

					grain = _lock.Grain;
					grains[grain.Identifier] = _lock;
				}
			}

			return grain;
		}

		/// <summary>
		/// Release resource lock as specified in the <paramref name="grain"/> object.
		/// </summary>
		/// <param name="grain">Object containing information regarding the lock obtained on a resource</param>
		public void Release(Grain grain)
		{
			if (grains.ContainsKey(grain.Identifier))
			{
				GrainComplex _grain = null;
				
				if(  grains.TryGetValue(grain.Identifier, out _grain) )
					_grain.Return();

				accessList[grain.Resource] = DateTime.Now;
			}
		}


		/// <summary>
		/// Optimize server by trimming resource list by removing least recently locked resources.
		/// </summary>
		public void Optimize()
		{
			lock (trimSync)
			{
				/// Remove 'stale' items from list without impeding lock/release requests, while accepting possible time taken for removals.
				/// This is done by putting together a list of removal candidates which is then traversed for actual confirm-and-remove
				
				TimeSpan threshold = new TimeSpan(0, 0, 25);

				List<Resource> removalList = new List<Resource>();

				ParallelOptions parallelOptions = new ParallelOptions();
				parallelOptions.MaxDegreeOfParallelism = accessList.Count < 20? 1:(accessList.Count / 20);

				// Compile a list of candidates
				ParallelLoopResult loopResult = Parallel.ForEach<string>(
					accessList.Keys, parallelOptions, key => {
						// no read lock is necessary here as this is the only block of code where removal of item from list is done and newly added item is irrelevant.

						DateTime lastAccessTimestamp;

						if (accessList.TryGetValue(key, out lastAccessTimestamp))
							if (DateTime.Now.Subtract(lastAccessTimestamp) > threshold)
								removalList.Add(resources[key]);
					}
				);

				//parallelOptions.CancellationToken.WaitHandle.WaitOne();

				Resource item;

				// Traverse candidate list to confirm and remove
				for( int idx = 0; idx < removalList.Count; idx++ )
				{
					item = removalList[idx];

					Mutex itemLock = item.SyncLock;

					// lock item to prevent lock/release 
					bool itemLocked = itemLock.WaitOne();

					if (DateTime.Now.Subtract(item.LastAccessTimestamp) > threshold)
					{
						try
						{
							accessList.Remove(item.Identifier);
							resources.Remove(item.Identifier);
						}
						finally
						{
							itemLock.ReleaseMutex();
							itemLock.Dispose();
						}
					}
				}

				int minThreads = (int)Math.Ceiling(grains.Count * 1.5);
				int maxThreads = minThreads * 2;
                ThreadPool.SetMaxThreads(maxThreads, maxThreads);
				//ThreadPool.SetMinThreads(minThreads, minThreads);
			}
		}
	}
}
