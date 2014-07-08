using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Bmbsqd.Async
{
	public class AsyncLock : IAwaitable<IDisposable>, INoCapturedContextAwaitable<IDisposable>
	{
		private object _current;
		private readonly ConcurrentQueue<AsyncLockWaiter> _waiters;

		public AsyncLock()
		{
			_waiters = new ConcurrentQueue<AsyncLockWaiter>();
		}

		internal void Done( AsyncLockWaiter waiter )
		{
			var old = Interlocked.Exchange( ref _current, null );
			if( old != waiter ) {
				Debug.Assert( false, "Invalid end state", "Expected current waiter to be {0} but was {1}", waiter, old );
			}
			TryNext();
		}

		private void TryNext()
		{
			if( TryTakeControl() ) {
				AsyncLockWaiter waiter;
				if( _waiters.TryDequeue( out waiter ) ) {
					RunWaiter( waiter );
				}
				else {
					ReleaseControl();
				}
			}
		}

		private void ReleaseControl()
		{
			if( Interlocked.CompareExchange( ref _current, null, Sentinel.Value ) != Sentinel.Value ) {
				Debug.Assert( false, "Invalid revert state", "Expected current waiter to be {0} but was {1}", Sentinel.Value, _current );
			}
		}

		private bool TryTakeControl()
		{
			return Interlocked.CompareExchange( ref _current, Sentinel.Value, null ) == null;
		}

		private void RunWaiter( AsyncLockWaiter waiter )
		{
			if( Interlocked.Exchange( ref _current, waiter ) != Sentinel.Value ) {
				Debug.Assert( false, "Invalid start state", "Expected current waiter to be {0} but was {1}", Sentinel.Value, _current );
			}
			waiter.Ready();
		}

		public INoCapturedContextAwaitable<IDisposable> WithoutContext
		{
			get { return this; }
		}

		private IAwaiter<IDisposable> GetAwaiter( ExecutionContext executionContext )
		{
			var waiter = new AsyncLockWaiter( this, executionContext );
			if( TryTakeControl() ) {
				RunWaiter( waiter );
			}
			else {
				_waiters.Enqueue( waiter );
				TryNext();
			}
			return waiter;
		}

		IAwaiter<IDisposable> INoCapturedContextAwaitable<IDisposable>.GetAwaiter()
		{
			return GetAwaiter( null );
		}

		public IAwaiter<IDisposable> GetAwaiter()
		{
			var executionContext = ExecutionContext.Capture();
			return GetAwaiter( executionContext );
		}
	}
}
