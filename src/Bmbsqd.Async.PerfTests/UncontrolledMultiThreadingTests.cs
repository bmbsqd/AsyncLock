using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Bmbsqd.Async.PerfTests
{
	[TestFixture]
	public class UncontrolledMultiThreadingTests : PerformanceTestBase
	{
		const int Threads = 1000 * 1000;

		private static async Task<double> RunTest( TestBase subject )
		{
			Func<TestBase, Task> f = async t => {
				for( var i = 0; i < 10; i++ ) {
					await t.LockOnce();
				}
			};

			/* <warmup> */
			for( var i = 0; i < 3; i++ )
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

		[TestFixtureSetUp]
		public void Setup()
		{
			Assert.That( SynchronizationContext.Current, Is.Null );
			Assert.That( TaskScheduler.Current.GetType().FullName, Is.EqualTo( "System.Threading.Tasks.ThreadPoolTaskScheduler" ) );

			// warm up the threadpool
			var waiting = 0;
			for( var i = 0; i < 5000; i++ ) {
				Interlocked.Increment( ref waiting );
				ThreadPool.UnsafeQueueUserWorkItem( ( x ) => {
					Thread.Sleep( 10 );
					Interlocked.Decrement( ref waiting );
				}, null ); // Force threadpool to fully populate with workers.. 
			}
			for( var i = 0; i < 100; i++ ) {
				if( waiting == 0 )
					break;
				Thread.Sleep( 100 );
			}
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