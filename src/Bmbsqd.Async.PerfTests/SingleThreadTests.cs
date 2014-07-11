using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Bmbsqd.Async.PerfTests
{
	[TestFixture]
	public class SingleThreadTests : PerformanceTestBase
	{
		const int Iterations = 1000 * 1000 * 40;

		[TearDown]
		public void Cleanup()
		{
			GC.Collect( GC.MaxGeneration, GCCollectionMode.Forced );
			GC.WaitForFullGCComplete();
		}

		[Test]
		public async Task NativeLock()
		{
			var _lock = new object();
			lock( _lock ) {
				// warmup
			}

			var start = Stopwatch.GetTimestamp();
			for( var i = 0; i < Iterations; i++ ) {
				lock( _lock ) {
					i--;
					i++;
				}
			}
			ReportTime( start );
		}

		[Test]
		public async Task AsyncLock()
		{
			var _lock = new AsyncLock();
			using( await _lock ) {
				// warmup
			}

			var start = Stopwatch.GetTimestamp();
			for( var i = 0; i < Iterations; i++ ) {
				using( await _lock ) {
					i--;
					i++;
				}
			}
			ReportTime( start );
		}

		[Test]
		public async Task NitoAsyncLock()
		{
			var _lock = new Nito.AsyncEx.AsyncLock();
			using( await _lock.LockAsync() ) {
				// warmup
			}

			var start = Stopwatch.GetTimestamp();
			for( var i = 0; i < Iterations; i++ ) {
				using( await _lock.LockAsync() ) {
					i--;
					i++;
				}
			}
			ReportTime( start );
		}

		[Test]
		public async Task SemaphoreSlimLock()
		{
			var _lock = new SemaphoreSlim( 1 );
			await _lock.WaitAsync();
			_lock.Release();

			var start = Stopwatch.GetTimestamp();
			for( var i = 0; i < Iterations; i++ ) {
				await _lock.WaitAsync();
				_lock.Release();
					i--;
					i++;
			}
			ReportTime( start );
		}

		private static void ReportTime( long start, [CallerMemberName] string name = null )
		{
			var time = (Stopwatch.GetTimestamp() - start) / (double)Stopwatch.Frequency;
			Console.WriteLine( "{0,30}: {1:0.000}", name, time );
		}
	}
}
