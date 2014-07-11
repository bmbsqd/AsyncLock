#region Creative Commons, Attribution-ShareAlike 3.0 Unported
/*
	Creative Commons
	Attribution-ShareAlike 3.0 Unported

	http://creativecommons.org/licenses/by-sa/3.0/
*/
#endregion

using System;
using System.Diagnostics;
using System.Threading;

namespace Bmbsqd.Async
{
	internal sealed class AsyncLockWaiter : WaiterBase
	{
		private static readonly Action _marker = () => { };
		private struct State
		{
			public const int Waiting = 0;
			public const int Running = 1;
			public const int Done = 2;
		}

		private class ContextAndAction
		{
			private readonly ExecutionContext _context;
			private readonly Action _continuation;

			public ContextAndAction( ExecutionContext context, Action continuation )
			{
				_context = context;
				_continuation = continuation;
			}

			public ExecutionContext Context { get { return _context; } }
			public Action Continuation { get { return _continuation; } }
		}

		private Action _continuation;
		private int _state;
		private ExecutionContext _executionContext;

		public override void Ready()
		{
			Debug.Assert( _state == State.Waiting, "Unexpected state", "Expected state to be {0} but was {1}", State.Waiting, _state );
			_state = State.Running;
			var continuation = Interlocked.Exchange( ref _continuation, _marker );
			ScheduleContinuation( _executionContext, continuation );
		}

		private static void ContinuationCallback( object state )
		{
			var c = (ContextAndAction)state;
			if( c.Context != null ) {
				ExecutionContext.Run( c.Context, x => ((Action)x)(), c.Continuation );
				c.Context.Dispose();
			}
			else {
				c.Continuation();
			}
		}

		private static void ScheduleContinuation( ExecutionContext executionContext, Action continuation )
		{
			if( continuation == null || continuation == _marker )
				return;

			var callbackState = new ContextAndAction( executionContext, continuation );
			ThreadPool.UnsafeQueueUserWorkItem( ContinuationCallback, callbackState );
		}

		public AsyncLockWaiter( AsyncLock @lock )
			: base( @lock )
		{
		}

		protected override void OnCompleted( Action continuation, bool captureExecutionContext )
		{
			Debug.Assert( _state != State.Running, "OnComplete() should never be called when state == Running" );
			if( _state == State.Waiting ) {

				if( captureExecutionContext ) {
					_executionContext = ExecutionContext.Capture();
				}

				var placeholder = Interlocked.Exchange( ref _continuation, continuation );

				if( placeholder == _marker ) {
					// Between here and above state==Waiting check, 
					// the Read() method has been called from another 
					// thread, we should schedule the continuation 
					// directly
					ScheduleContinuation( _executionContext, continuation );
				}
			}
			else {
				continuation();
			}
		}

		public override bool IsCompleted // this is the AWAIT complete, not the waiter block
		{
			get { return _state != State.Waiting; }
		}

		public override void Dispose()
		{
			Debug.Assert( _state == State.Running, "Dispose() should only be called on a Running state" );
			_state = State.Done;
			base.Dispose();
		}

		public override string ToString()
		{
			return "AsyncWaiter: " + base.ToString();
		}
	}
}