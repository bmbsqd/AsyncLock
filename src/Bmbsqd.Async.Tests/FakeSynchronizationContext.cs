using System;
using System.Threading;

namespace Bmbsqd.Async.Tests
{
	public class FakeSynchronizationContext : SynchronizationContext
	{
		public int OperationsPosted { get; private set; }

		public override void Post(SendOrPostCallback d, object state)
		{
			OperationsPosted++;
			Console.WriteLine("Posted");
			base.Post(d, state);
		}

		public override void Send(SendOrPostCallback d, object state)
		{
			OperationsPosted++;
			Console.WriteLine("Sent");
			base.Send(d, state);
		}

		public override string ToString() => "FAKE";
	}
}