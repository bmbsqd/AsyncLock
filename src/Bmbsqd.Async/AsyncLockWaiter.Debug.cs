#if DEBUG
using System.Diagnostics;
using System.Threading;
#endif

namespace Bmbsqd.Async
{
	internal sealed partial class AsyncLockWaiter
	{
		private struct State
		{
			public const int Waiting = 0;
			public const int Running = 1;
			public const int Done = 2;
		}


		partial void ChangeState( int expectedState, int newState, string unexpectedStateMessage );
#if DEBUG
		private int _state;
		partial void ChangeState( int expectedState, int newState, string unexpectedStateMessage )
		{

			var oldState = Interlocked.Exchange( ref _state, newState );
			Debug.Assert( oldState == expectedState, unexpectedStateMessage );
		}
#endif
	}
}