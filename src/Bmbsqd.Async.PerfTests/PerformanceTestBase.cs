using System;
using System.Diagnostics;
using NUnit.Framework;

namespace Bmbsqd.Async.PerfTests
{
	public class PerformanceTestBase
	{
		[SetUp]
		public void MakeSureThisIsReleaseConfiguration()
		{
			AssertNotDebug();
		}

		[Conditional( "DEBUG" )]
		private static void AssertNotDebug()
		{
			Console.WriteLine( "WARNING: Current build seems to be DEBUG. Switch to RELEASE and try again" );
		}
	}
}