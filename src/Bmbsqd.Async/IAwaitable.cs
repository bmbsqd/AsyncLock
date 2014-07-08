namespace Bmbsqd.Async
{
	public interface IAwaitable<out TResult>
	{
		IAwaiter<TResult> GetAwaiter();
	}
}