using System.Runtime.CompilerServices;

namespace Bmbsqd.Async
{
	public interface IAwaiter<out TResult> : INotifyCompletion, ICriticalNotifyCompletion
	{
		bool IsCompleted { get; }
		TResult GetResult();
	}
}