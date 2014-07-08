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
			private readonly ExecutionContext _executionContext;
			private readonly Action _continuation;

			public ContextAndAction( ExecutionContext executionContext, Action continuation )
			{
				_executionContext = executionContext;
				_continuation = continuation;
			}

			public ExecutionContext ExecutionContext { get { return _executionContext; } }
			public Action Continuation { get { return _continuation; } }
		}


		private Action _continuation;
		private int _state;
		private readonly AsyncLock _lock;
		private readonly ExecutionContext _executionContext;

		public void Ready()
		{
			if( Interlocked.CompareExchange( ref _state, State.Running, State.Waiting ) == State.Waiting ) {
				var continuation = Interlocked.Exchange( ref _continuation, null );
				if( continuation != null ) {
					ScheduleContinuation( _executionContext, continuation );
				}
			}
		}

		private static void ScheduleContinuation( ExecutionContext executionContext, Action continuation )
		{
			if( executionContext != null ) {
				ThreadPool.UnsafeQueueUserWorkItem( state => {
					var c = (ContextAndAction)state;
					ExecutionContext.Run( c.ExecutionContext, cont => ((Action)cont)(), c.Continuation );
					executionContext.Dispose();
				}, new ContextAndAction( executionContext, continuation ) );
			}
			else {
				ThreadPool.UnsafeQueueUserWorkItem( state => ((Action)state)(), continuation );
			}
		}

		public AsyncLockWaiter( AsyncLock @lock, ExecutionContext executionContext )
		{
			_lock = @lock;
			_executionContext = executionContext;
		}

		public void OnCompleted( Action continuation )
		{
			if( _state == State.Waiting ) {
				_continuation = continuation;
			}
			else if( continuation != null ) {
				continuation();
			}
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