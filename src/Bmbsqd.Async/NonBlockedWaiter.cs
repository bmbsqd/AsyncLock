using System;

namespace Bmbsqd.Async
{
	internal sealed class NonBlockedWaiter : WaiterBase
	{
		public NonBlockedWaiter( AsyncLock @lock )
			: base( @lock )
		{
		}

		protected override void OnCompleted( Action continuation, bool captureExecutionContext )
		{
			if( continuation != null ) {
				continuation();
			}
		}

		public override bool IsCompleted
		{
			get { return true; }
		}

		public override string ToString()
		{
			return "NonBlockingWaiter: " + base.ToString();
		}
	}
}