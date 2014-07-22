#region Creative Commons, Attribution-ShareAlike 3.0 Unported
/*
	Creative Commons
	Attribution-ShareAlike 3.0 Unported

	http://creativecommons.org/licenses/by-sa/3.0/
*/
#endregion

using System;
using System.Threading;

namespace Bmbsqd.Async
{
	internal sealed partial class AsyncLockWaiter : WaiterBase
	{
		private static readonly Action _marker = () => { };

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

		private Action _continuation;
		private ExecutionContext _executionContext;

		public AsyncLockWaiter( AsyncLock @lock )
			: base( @lock )
		{
		}

		public override void Ready()
		{
			ChangeState( State.Waiting, State.Running, "Unexpected state: Should be Waiting" );
			var continuation = Interlocked.Exchange( ref _continuation, _marker );
			ScheduleContinuation( _executionContext, continuation );
		}

		protected override void OnCompleted( Action continuation, bool captureExecutionContext )
		{
			if( captureExecutionContext ) {
				_executionContext = ExecutionContext.Capture();
			}

			var placeholder = Interlocked.Exchange( ref _continuation, continuation );
			if( placeholder == _marker ) {
				// Between start of this method and $here, 
				// the Ready() method have been called from another 
				// thread, we should schedule the continuation 
				// directly
				ScheduleContinuation( _executionContext, continuation );
			}
		}

		public override bool IsCompleted
		{
			// since this is the async waiter, we will never 
			// be complete here, and even if we would be, 
			// the code would still behave correct
			get { return false; }
		}

		public override void Dispose()
		{
			ChangeState( State.Running, State.Done, "Unexpected state: Should be Running" );
			base.Dispose();
		}

		public override string ToString()
		{
			return "AsyncWaiter: " + base.ToString();
		}
	}
}