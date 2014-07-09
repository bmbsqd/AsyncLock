#region Creative Commons, Attribution-ShareAlike 3.0 Unported
/*
	Creative Commons
	Attribution-ShareAlike 3.0 Unported

	http://creativecommons.org/licenses/by-sa/3.0/
*/
#endregion

using System;
using System.Globalization;
using System.Threading;

namespace Bmbsqd.Async
{
	internal class AsyncLockWaiter : IAwaiter<IDisposable>, IDisposable
	{
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
		private readonly AsyncLock _lock;
		private ExecutionContext _executionContext;

		public void Ready( bool synchronously )
		{
			if( Interlocked.CompareExchange( ref _state, State.Running, State.Waiting ) == State.Waiting ) {
				var continuation = Interlocked.Exchange( ref _continuation, null );
				if( continuation != null ) {
					ScheduleContinuation( _executionContext, continuation, synchronously );
				}
			}
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

		private static void ScheduleContinuation( ExecutionContext executionContext, Action continuation, bool synchronously )
		{
			if( synchronously ) {
				// This could probably never happen as the OnComplete() would be 
				// called after GetAwaiter() and there would be no continuation 
				// to execute at this point.. 
				continuation();
			}
			else {
				var callbackState = new ContextAndAction( executionContext, continuation );
				ThreadPool.UnsafeQueueUserWorkItem( ContinuationCallback, callbackState );
			}
		}

		public AsyncLockWaiter( AsyncLock @lock )
		{
			_lock = @lock;
		}

		private void OnCompleted( Action continuation, bool captureExecutionContext )
		{
			if( _state == State.Waiting ) {
				_continuation = continuation;
				if( captureExecutionContext ) {
					_executionContext = ExecutionContext.Capture();
				}
			}
			else if( continuation != null ) {
				continuation();
			}
		}

		public void OnCompleted( Action continuation )
		{
			OnCompleted( continuation, true );
		}

		public void UnsafeOnCompleted( Action continuation )
		{
			OnCompleted( continuation, false );
		}

		public bool IsCompleted
		{
			get { return _state != State.Waiting; }
		}

		public IDisposable GetResult()
		{
			return this;
		}

		public void Dispose()
		{
			if( Interlocked.CompareExchange( ref _state, State.Done, State.Running ) == State.Running ) {
				_lock.Done( this );
			}
		}

		public override string ToString()
		{
			return GetHashCode().ToString( "x8", CultureInfo.InvariantCulture );
		}
	}
}