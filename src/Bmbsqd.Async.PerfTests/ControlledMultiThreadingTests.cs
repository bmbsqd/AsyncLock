using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Bmbsqd.Async.PerfTests
{
	[TestFixture]
	public class ControlledMultiThreadingTests : PerformanceTestBase
	{
		const int Iterations = 1000 * 1000;
		const int Threads = 10;


		private static async Task<double> RunTest( TestBase subject )
		{
			Func<TestBase, Task> f = async t => {
				for( var i = 0; i < Iterations; i ++ ) {
					await t.LockOnce();
				}
			};

			/* <warmup> */
			for( var i = 0 ; i < 10; i++ )
				await f( subject );
			/* </warmup> */
			
			var start = Stopwatch.GetTimestamp();

			var tasks = Enumerable
				.Range( 0, Threads )
				.Select( i => f( subject ) )
				.ToList();
			await Task.WhenAll( tasks );

			return (Stopwatch.GetTimestamp() - start) / (double)Stopwatch.Frequency;
		}


		[TearDown]
		public void Cleanup()
		{
			GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
			GC.WaitForFullGCComplete();
		}

		//[Test]
		//public async Task NativeLock()
		//{
		//	ReportTime( await RunTest( new NativeLockTest() ) );
		//}

		[Test]
		public async Task AsyncLock()
		{
			ReportTime( await RunTest( new AsyncLockTest() ) );
		}

		[Test]
		public async Task SemaphoreSlimLock()
		{
			ReportTime( await RunTest( new SemaphoreLockTest() ) );
		}

		[Test]
		public async Task NitoAsyncLock()
		{
			ReportTime( await RunTest( new NitoAsyncLockTest() ) );
		}

		private static void ReportTime( double time, [CallerMemberName] string name = null )
		{
			Console.WriteLine( "{0,30}: {1:0.000}", name, time );
		}
	}
}