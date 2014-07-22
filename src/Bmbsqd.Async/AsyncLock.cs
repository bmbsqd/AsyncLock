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
	[DebuggerDisplay( "HasLock = {HasLock}, Waiting = {WaitingCount}" )]
	public class AsyncLock : IAwaitable<IDisposable>
	{
		private object _current;
		private readonly ConcurrentQueue<WaiterBase> _waiters;

		public AsyncLock()
		{
			_waiters = new ConcurrentQueue<WaiterBase>();
		}

		internal void Done( WaiterBase waiter )
		{
			var oldWaiter = Interlocked.Exchange( ref _current, null );
			Debug.Assert( oldWaiter == waiter, "Invalid end state", "Expected current waiter to be {0} but was {1}", waiter, oldWaiter );
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
			if( Interlocked.Exchange( ref _current, null ) != Sentinel.Value ) {
				Debug.Assert( false, "Invalid revert state", "Expected current waiter to be {0} but was {1}", Sentinel.Value, _current );
			}
		}

		private bool TryTakeControl()
		{
			return Interlocked.CompareExchange( ref _current, Sentinel.Value, null ) == null;
		}

		private void RunWaiter( WaiterBase waiter )
		{
			Debug.Assert( _current == Sentinel.Value, "Invalid start state", "Expected current waiter to be {0} but was {1}", Sentinel.Value, _current );
			_current = waiter;
			waiter.Ready();
		}

		public bool HasLock
		{
			get { return _current != null; }
		}

		private int WaitingCount
		{
			// only used in debug view
			get { return _waiters.Count; }
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

		public override string ToString()
		{
			return "AsyncLock: " + (HasLock ? "Locked with " + WaitingCount + " queued waiters" : "Unlocked");
		}
	}
}
