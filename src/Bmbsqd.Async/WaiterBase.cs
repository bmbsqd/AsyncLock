using System;
using System.Globalization;

namespace Bmbsqd.Async
{
	internal abstract class WaiterBase : IAwaiter<IDisposable>, IDisposable
	{
		protected readonly AsyncLock _lock;

		protected WaiterBase( AsyncLock @lock )
		{
			_lock = @lock;
		}

		public abstract bool IsCompleted { get; }

		public IDisposable GetResult()
		{
			return this;
		}

		public virtual void Ready()
		{
		}

		public virtual void Dispose()
		{
			_lock.Done( this );
		}

		public override string ToString()
		{
			return GetHashCode().ToString( "x8", CultureInfo.InvariantCulture );
		}

		protected abstract void OnCompleted( Action continuation, bool captureExecutionContext );

		public void OnCompleted( Action continuation )
		{
			OnCompleted( continuation, true );
		}

		public void UnsafeOnCompleted( Action continuation )
		{
			OnCompleted( continuation, false );
		}
	}
}