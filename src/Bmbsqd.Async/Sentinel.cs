namespace Bmbsqd.Async
{
	internal class Sentinel
	{
		public static readonly object Value = new Sentinel();

		public override string ToString() => GetType().Name;
	}
}