using System;
using System.Threading;

namespace Bmbsqd.Async.Tests
{
	public class FakeSynchronizationContext : SynchronizationContext
	{
		private int _operationsPosted;
		public override void Post( SendOrPostCallback d, object state )
		{
			_operationsPosted++;
			Console.WriteLine( "Posted" );
			base.Post( d, state );
		}

		public override void Send( SendOrPostCallback d, object state )
		{
			_operationsPosted++;
			Console.WriteLine( "Sent" );
			base.Send( d, state );
		}

		public int OperationsPosted
		{
			get { return _operationsPosted; }
		}

		public override string ToString()
		{
			return "FAKE";
		}
	}
}