using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using System.Diagnostics;

using System.Security.Cryptography;

using Pug.Cereal;

namespace ConsoleTester
{
	class Program
	{
		static void Main(string[] args)
		{
			//ECDsaCng cng = new ECDsaCng(256);
			//RandomNumberGenerator rng = RandomNumberGenerator.Create();
			//cng.HashAlgorithm = CngAlgorithm.Sha256;

			//int dataLength = 1024; // 256 * 256;

			//byte[] data = new byte[dataLength];
			//rng.GetBytes(data);

			//DateTime begining, middle, end;

			//begining = DateTime.Now;
			//byte[] signature = cng.SignData(data);

			//middle = DateTime.Now;

			//bool authentic = cng.VerifyData(data, signature);

			//end = DateTime.Now;

			//Debug.WriteLine(middle.Subtract(begining).TotalMilliseconds);
			//Debug.WriteLine(end.Subtract(middle).TotalMilliseconds);

			//MACTripleDES tripleDes = new MACTripleDES();

			//begining = DateTime.Now;

			//byte[] hash = tripleDes.ComputeHash(data);

			//middle = DateTime.Now;

			//Debug.WriteLine(middle.Subtract(begining).TotalMilliseconds);

			//qvCjITx03WrvGVYEge0G9YJHqXZApuZp

			Cereal cereal = new Cereal("TEST", 500);

			Func<string, string, int, IGrain> getLock = new Func<string, string, int, IGrain>(
				(subject, resource, duration) =>
				{
					Console.WriteLine("{0} a", subject);

					IGrain grain = cereal.Lock(subject, resource, -1);

					Console.WriteLine("{0} b", subject);

					if (!grain.Equals(Grain.Empty))
					{
						Console.WriteLine("{0} OBTAINED lock for object {1}.", subject, resource);
					}
					else
					{
						Console.WriteLine("{0} UNABLE to obtain lock for object {1}.", subject, resource);
					}

					Console.WriteLine("{0} c", subject);


					return grain;
                }
			);

			Action<IGrain> release = new Action<IGrain>((grain) => { cereal.Release(grain); Console.WriteLine("{0} released lock for object {1}.", grain.Subject, grain.Resource); });
			//LastAccessTimestamp = DateTime.Now;


			Thread thread1 = new Thread(
									new ThreadStart(
										() =>
										{
											Console.WriteLine("Thread 1 started.");

											DateTime start = DateTime.Now;
											IGrain lockA = getLock("Thread1", "A", 4000);

											DateTime finish = DateTime.Now;
											TimeSpan elpsed = finish.Subtract(start);

											Thread.Sleep(2000);

											Task.Run( () => release(lockA) ).Wait();

											Thread.Sleep(2000);

											lockA = Grain.Empty;

											while(lockA .Equals(Grain.Empty))
											{
												lockA = getLock("Thread1", "A", 2000);
											}
										}
									)
								);

			Thread thread2 = new Thread(
									new ThreadStart(
										() =>
										{
											Console.WriteLine("Thread 2 started.");

											IGrain lockA = getLock("Thread2", "A", 5000);

											if( lockA.Equals(Grain.Empty))
											{
												Thread.Sleep(2000);
												lockA = getLock("Thread2", "A", 5000);
											}

											Thread.Sleep(250);
											lockA = getLock("Thread2", "A",50001);

											Thread.Sleep(2000);

											release(lockA);
										}
									)
								);

			thread1.Start();
			Thread.Sleep(250);
			thread2.Start();

			Thread.Sleep(60000);

			cereal.Optimize();

			Console.ReadLine();
		}
	}
}
