using System;

using NUnit.Framework;

using Pug.Cereal;
using System.Threading;
using System.Threading.Tasks;

namespace UnitTests
{
	[TestFixture]
	public class NUnitTest
	{
		ICereal cereal;

		[OneTimeSetUp]
		public void Setup()
		{
		}

		[Test]
		[Order(100)]
		public void StandardTest()
		{
			cereal = new Cereal("Test");

			Func<string, string, int, IGrain> getLock = new Func<string, string, int, IGrain>(
				(subject, resource, duration) =>
				{
					Console.WriteLine("{2}: {0} requesting lock for object {1}.", subject, resource, DateTime.Now.ToString("o"));

					IGrain grain = cereal.Lock(subject, resource, -1);

					if (!grain.Equals(Grain.Empty))
					{
						Console.WriteLine("{2}: {0} OBTAINED lock for object {1}.", subject, resource, DateTime.Now.ToString("o"));
					}
					else
					{
						Console.WriteLine("{2}: {0} UNABLE to obtain lock for object {1}.", subject, resource, DateTime.Now.ToString("o"));
					}

					return grain;
				}
			);

			Action<IGrain> release = new Action<IGrain>(
				(grain) =>
				{
					Console.WriteLine("{2}: {0} releasing lock for object {1}.", grain.Subject, grain.Resource, DateTime.Now.ToString("o"));

					cereal.Release(grain);

					Console.WriteLine("{2}: {0} released lock for object {1}.", grain.Subject, grain.Resource, DateTime.Now.ToString("o"));
				}
			);

			Task first = Task.Run(
									() =>
									{
										Console.WriteLine("Thread 1 started.");

										DateTime start = DateTime.Now;
										IGrain lockA = getLock("Thread1", "A", 4000);

										DateTime finish = DateTime.Now;
										TimeSpan elpsed = finish.Subtract(start);

										Thread.Sleep(2000);

										Task.Run(() => release(lockA)).Wait();

										Thread.Sleep(2000);

										lockA = Grain.Empty;

										while (lockA.Equals(Grain.Empty))
										{
											lockA = getLock("Thread1", "A", 2000);
										}

										release(lockA);
									}
								);

			Thread.Sleep(250);

			Task second = Task.Run(
									() =>
									{
										Console.WriteLine("Thread 2 started.");

										IGrain lockA = getLock("Thread2", "A", 5000);

										if (lockA.Equals(Grain.Empty))
										{
											Thread.Sleep(2000);
											lockA = getLock("Thread2", "A", 5000);
										}

										Thread.Sleep(250);
										lockA = getLock("Thread2", "A", 50001);

										Thread.Sleep(2000);

										release(lockA);
									}
								);

			first.Wait();
			second.Wait();

			Assert.IsTrue(first.IsCompleted && !first.IsFaulted);
			Assert.IsTrue(second.IsCompleted && !second.IsFaulted);
		}

		[Test(Description = "PossibleDeadlock should not occur if lock function is called with a finite timeout.")]
		[Order(200)]
		public void DeadlockDetectionNegativeTriggerTest()
		{
			cereal = new Cereal("Test");

			if (!cereal.HasDeadlockDetection)
			{
				Assert.Pass("Deadlock detection not available.");
				return;
			}

			IGrain firstAGrain = cereal.Lock("first", "A");

			Assert.NotNull(firstAGrain);
			Assert.AreNotEqual(firstAGrain, Grain.Empty);

			IGrain secondBGrain = cereal.Lock("second", "B");

			Assert.NotNull(secondBGrain);
			Assert.AreNotEqual(secondBGrain, Grain.Empty);

			Task<IGrain> firstBGrainLock = Task.Run<IGrain>(() => cereal.Lock("first", "B"));

			Assert.DoesNotThrowAsync(
				new AsyncTestDelegate(
					() =>
					{
						return Task.Run(
							() => cereal.Lock("second", "A", 0)
						);
					}

				)
			);

			cereal.Release(secondBGrain);

			firstBGrainLock.Wait();

			Assert.IsTrue(firstBGrainLock.IsCompleted && !firstBGrainLock.IsFaulted);
			Assert.IsTrue(firstBGrainLock.Result != null);

			cereal.Release(firstAGrain);
			cereal.Release(firstBGrainLock.Result);
		}

		[Test(Description = "PossibleDeadlock should occur if lock function is called with infinite timeout.")]
		[Order(300)]
		public void DeadlockDetectionTest()
		{
			cereal = new Cereal("Test");

			if (!cereal.HasDeadlockDetection )
			{
				Assert.Pass("Deadlock detection not available.");
				return;
			}

			IGrain firstAGrain = cereal.Lock("first", "A");

			Assert.NotNull(firstAGrain);
			Assert.AreNotEqual(firstAGrain, Grain.Empty);

			IGrain secondBGrain = cereal.Lock("second", "B");

			Assert.NotNull(secondBGrain);
			Assert.AreNotEqual(secondBGrain, Grain.Empty);
			
			Task<IGrain> firstBGrainLock = Task.Run<IGrain>(() => cereal.Lock("first", "B"));

			//Assert.Throws(typeof(PossibleDeadlock), new TestDelegate(() => cereal.Lock("second", "A")));

			Assert.ThrowsAsync(
				typeof(PossibleDeadlock),
				new AsyncTestDelegate(
					() =>
					{
						return Task.Run(
							() => cereal.Lock("second", "A")
						);
					}

				)
			);

			cereal.Release(secondBGrain);

			firstBGrainLock.Wait();

			Assert.IsTrue(firstBGrainLock.IsCompleted && !firstBGrainLock.IsFaulted);
			Assert.IsTrue(firstBGrainLock.Result != null);

			cereal.Release(firstAGrain);
			cereal.Release(firstBGrainLock.Result);
		}
	}
}