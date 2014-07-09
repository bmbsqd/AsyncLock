#region Creative Commons, Attribution-ShareAlike 3.0 Unported
/*
	Creative Commons
	Attribution-ShareAlike 3.0 Unported

	http://creativecommons.org/licenses/by-sa/3.0/
*/
#endregion

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Bmbsqd.Async
{
	public class AsyncLock : IAwaitable<IDisposable>
	{
		private object _current;
		private readonly ConcurrentQueue<WaiterBase> _waiters;

		public AsyncLock()
		{
			_waiters = new ConcurrentQueue<WaiterBase>();
		}

		internal void Done( IAwaiter<IDisposable> waiter )
		{
			var oldWaiter = Interlocked.CompareExchange( ref _current, null, waiter );
			if( oldWaiter != waiter ) {
				Debug.Assert( false, "Invalid end state", "Expected current waiter to be {0} but was {1}", waiter, oldWaiter );
			}
			TryNext();
		}

		private void TryNext()
		{
			if( TryTakeControl() ) {
				WaiterBase waiter;
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

		private void RunWaiter( WaiterBase waiter )
		{
			if( Interlocked.Exchange( ref _current, waiter ) != Sentinel.Value ) {
				Debug.Assert( false, "Invalid start state", "Expected current waiter to be {0} but was {1}", Sentinel.Value, _current );
			}
			waiter.Ready();
		}

		public bool HasLock
		{
			get { return _current != null; }
		}

		public IAwaiter<IDisposable> GetAwaiter()
		{
			WaiterBase waiter;
			if( TryTakeControl() ) {
				waiter = new NonBlockedWaiter( this );
				RunWaiter( waiter );
				
			}
			else {
				waiter = new AsyncLockWaiter( this );
				_waiters.Enqueue( waiter );
				TryNext();
			}
			return waiter;
		}
	}
}
