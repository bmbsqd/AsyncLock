using System.Collections.Generic;
using System.Diagnostics;

namespace Bmbsqd.Async.Tests
{
	public static class StackHelper
	{
		public static IEnumerable<StackFrame> CurrentCallStack
		{
			get
			{
				var trace = new StackTrace( 1, false );
				return trace.GetFrames();
			}
		}

		public static string Text
		{
			get
			{
				return new StackTrace( 1, false ).ToString();
			}
		}
	
	}
}