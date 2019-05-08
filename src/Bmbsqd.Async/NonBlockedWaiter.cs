using System;

namespace Bmbsqd.Async
{
	internal sealed class NonBlockedWaiter : WaiterBase
	{
		public NonBlockedWaiter(AsyncLock @lock)
			: base(@lock)
		{
		}

		public override bool IsCompleted => true;

		public override string ToString() => "NonBlockingWaiter: " + base.ToString();

		protected override void OnCompleted(Action continuation, bool captureExecutionContext) => continuation?.Invoke();
	}
}