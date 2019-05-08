using System;
using System.Globalization;

namespace Bmbsqd.Async
{
	internal abstract class WaiterBase : IAwaiter<IDisposable>, IDisposable
	{
		protected readonly AsyncLock _lock;

		protected WaiterBase(AsyncLock @lock) => _lock = @lock;

		public abstract bool IsCompleted { get; }

		public virtual void Dispose() => _lock.Done(this);

		public IDisposable GetResult() => this;

		public void OnCompleted(Action continuation) => OnCompleted(continuation, true);

		public virtual void Ready() { }

		public override string ToString() => GetHashCode().ToString("x8", CultureInfo.InvariantCulture);

		public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation, false);

		protected abstract void OnCompleted(Action continuation, bool captureExecutionContext);
	}
}