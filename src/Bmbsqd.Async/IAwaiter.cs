using System.Runtime.CompilerServices;

namespace Bmbsqd.Async
{
	public interface IAwaiter<out TResult> : INotifyCompletion
	{
		bool IsCompleted { get; }
		TResult GetResult();
	}
}