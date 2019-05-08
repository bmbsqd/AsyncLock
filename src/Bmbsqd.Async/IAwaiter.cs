using System.Runtime.CompilerServices;

namespace Bmbsqd.Async
{
	public interface IAwaiter<out TResult> : ICriticalNotifyCompletion
	{
		bool IsCompleted { get; }

		TResult GetResult();
	}
}