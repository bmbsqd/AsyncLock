namespace Bmbsqd.Async
{
	public interface INoCapturedContextAwaitable<out TResult>
	{
		IAwaiter<TResult> GetAwaiter();
	}
}