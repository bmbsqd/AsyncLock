using System.Threading;
using System.Threading.Tasks;

namespace Bmbsqd.Async.PerfTests
{
	public abstract class TestBase
	{
		public abstract Task LockOnce();

		protected Task Work()
		{
			for( var i = 0; i < 100; i++ ) { }
			return Task.FromResult( true );
		}
	}

	public class AsyncLockTest : TestBase
	{
		private readonly AsyncLock _lock;

		public AsyncLockTest()
		{
			_lock = new AsyncLock();
		}

		public override async Task LockOnce()
		{
			using( await _lock ) {
				await Work();
			}
		}
	}
	
	public class NitoAsyncLockTest : TestBase
	{
		private readonly Nito.AsyncEx.AsyncLock _lock;

		public NitoAsyncLockTest()
		{
			_lock = new Nito.AsyncEx.AsyncLock();
		}

		public override async Task LockOnce()
		{
			using( await _lock.LockAsync() ) {
				await Work();
			}
		}
	}

	public class SemaphoreLockTest : TestBase
	{
		private readonly SemaphoreSlim _lock;

		public SemaphoreLockTest()
		{
			_lock = new SemaphoreSlim( 1 );
		}

		public override async Task LockOnce()
		{
			await _lock.WaitAsync();
			await Work();
			_lock.Release();
		}
	}
}