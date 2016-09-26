#region MIT License
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
#endregion

using System;
using System.Threading;

namespace Bmbsqd.Async {
	internal sealed partial class AsyncLockWaiter : WaiterBase {
		private static readonly Action _marker = () => { };

		private class ContextAndAction {
			public ContextAndAction( ExecutionContext context, Action continuation )
			{
				Context = context;
				Continuation = continuation;
			}

			public ExecutionContext Context { get; }
			public Action Continuation { get; }
		}

		private static void ContinuationCallback( object state )
		{
			var c = (ContextAndAction)state;
			if( c.Context != null ) {
				ExecutionContext.Run( c.Context, x => ((Action)x)(), c.Continuation );
			} else {
				c.Continuation();
			}
		}

		private static void ScheduleContinuation( ExecutionContext executionContext, Action continuation )
		{
			if( continuation == null || continuation == _marker )
				return;

			var callbackState = new ContextAndAction( executionContext, continuation );
			ThreadPool.QueueUserWorkItem( ContinuationCallback, callbackState );
		}

		private Action _continuation;
		private ExecutionContext _executionContext;

		public AsyncLockWaiter( AsyncLock @lock ) : base( @lock )
		{ }

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

		public override string ToString() => "AsyncWaiter: " + base.ToString();
	}
}