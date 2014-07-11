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
	public class MultiThreadTests
	{
		const int Iterations = 1000 * 1000;
		const int Threads = 10;

		private interface ITestBase
		{
			Task LockOnce();
		}

		private class AsyncLockTest : ITestBase
		{
			private readonly AsyncLock _lock;

			public AsyncLockTest()
			{
				_lock = new AsyncLock();
			}

			public async Task LockOnce()
			{
				using( await _lock ) {
				}
			}
		}

		private class NativeLockTest : ITestBase
		{
			private readonly object _lock;

			public NativeLockTest()
			{
				_lock = new object();
			}

			public async Task LockOnce()
			{
				lock( _lock ) {
					
				}
			}
		}

		private class NitoAsyncLockTest : ITestBase
		{
			private readonly Nito.AsyncEx.AsyncLock _lock;

			public NitoAsyncLockTest()
			{
				_lock = new Nito.AsyncEx.AsyncLock();
			}

			public async Task LockOnce()
			{
				using( await _lock.LockAsync() ) {
				}
			}
		}

		private class SemaphoreLockTest : ITestBase
		{
			private readonly SemaphoreSlim _lock;

			public SemaphoreLockTest()
			{
				_lock = new SemaphoreSlim( 1 );
			}

			public async Task LockOnce()
			{
				await _lock.WaitAsync();
				_lock.Release();
			}
		}


		private static async Task<TimeSpan> RunTest( ITestBase subject )
		{
			Func<ITestBase, Task> f = async t => {
				for( var i = 0; i < Iterations; i ++ ) {
					await t.LockOnce();
				}
			};

			await f( subject );
			// warmup


			var start = Stopwatch.GetTimestamp();

			var tasks = Enumerable.Range( 0, Threads ).Select( i => f( subject ) );
			await Task.WhenAll( tasks );

			return TimeSpan.FromSeconds( (Stopwatch.GetTimestamp() - start) / (double)Stopwatch.Frequency );
		}


		[TearDown]
		public void Cleanup()
		{
			GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
			GC.WaitForFullGCComplete();
		}

		[Test]
		public async Task NativeLock()
		{
			ReportTime( await RunTest( new NativeLockTest() ) );
		}

		[Test]
		public async Task AsyncLock()
		{
			ReportTime( await RunTest( new AsyncLockTest() ) );
		}

		[Test]
		public async Task SemaphoreSlim()
		{
			ReportTime( await RunTest( new SemaphoreLockTest() ) );
		}

		[Test]
		public async Task NitoAsyncLock()
		{
			ReportTime( await RunTest( new NitoAsyncLockTest() ) );
		}

		private static void ReportTime( TimeSpan time, [CallerMemberName] string name = null )
		{
			Console.WriteLine( "{0,30}: {1}", name, time );
		}
	}
}