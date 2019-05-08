/*
MIT License

Copyright (c) 2016 Bombsquad Inc

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;

namespace Bmbsqd.Async
{
	[DebuggerDisplay("HasLock = {HasLock}, Waiting = {WaitingCount}")]
	public class AsyncLock : IAwaitable<IDisposable>
	{
		private readonly ConcurrentQueue<WaiterBase> _waiters;
		private object _current;

		public AsyncLock() => _waiters = new ConcurrentQueue<WaiterBase>();

		public bool HasLock => _current != null;

		// only used in debug view
		private int WaitingCount => _waiters.Count;

		public IAwaiter<IDisposable> GetAwaiter()
		{
			WaiterBase waiter;
			if (TryTakeControl())
			{
				waiter = new NonBlockedWaiter(this);
				RunWaiter(waiter);
			}
			else
			{
				waiter = new AsyncLockWaiter(this);
				_waiters.Enqueue(waiter);
				TryNext();
			}
			return waiter;
		}

		public override string ToString() => "AsyncLock: " + (HasLock ? "Locked with " + WaitingCount + " queued waiters" : "Unlocked");

		internal void Done(WaiterBase waiter)
		{
			var oldWaiter = Interlocked.Exchange(ref _current, null);
			Debug.Assert(oldWaiter == waiter, "Invalid end state", string.Format("Expected current waiter to be {0} but was {1}", waiter, oldWaiter));
			TryNext();
		}

		private void ReleaseControl()
		{
			if (Interlocked.Exchange(ref _current, null) != Sentinel.Value)
			{
				Debug.Assert(false, "Invalid revert state", string.Format("Expected current waiter to be {0} but was {1}", Sentinel.Value, _current));
			}
		}

		private void RunWaiter(WaiterBase waiter)
		{
			Debug.Assert(_current == Sentinel.Value, "Invalid start state", string.Format("Expected current waiter to be {0} but was {1}", Sentinel.Value, _current));
			_current = waiter;
			waiter.Ready();
		}

		private void TryNext()
		{
			if (TryTakeControl())
			{
				WaiterBase waiter;
				if (_waiters.TryDequeue(out waiter))
				{
					RunWaiter(waiter);
				}
				else
				{
					ReleaseControl();
				}
			}
		}

		private bool TryTakeControl() => Interlocked.CompareExchange(ref _current, Sentinel.Value, null) == null;
	}
}