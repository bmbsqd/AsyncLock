using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Bmbsqd.Async.Tests
{
	[TestFixture]
	public class AsyncLockTests
	{
		[Test]
		public async Task Hello()
		{
			var l = new AsyncLock();
			var concurrentCounter = 0;


			Func<Task> t = async () => {
				Console.WriteLine( "{0} ...", Thread.CurrentThread.ManagedThreadId );
				Thread.CurrentPrincipal = new FakePrincipal( "HELLO WORLD" );
				using( await l.WithoutContext ) {
					Console.WriteLine( "{0} In lock as {1}", Thread.CurrentThread.ManagedThreadId, Thread.CurrentPrincipal );
					await Task.Delay( 100 );
					if( Interlocked.Increment( ref concurrentCounter ) != 1 ) {
						Console.WriteLine( "{0} In Lock Inconsistency", Thread.CurrentThread.ManagedThreadId );
					}
					await Task.Delay( 100 );
				}
				Console.WriteLine( "{0} Out of lock", Thread.CurrentThread.ManagedThreadId );

				if( Interlocked.Decrement( ref concurrentCounter ) != 0 ) {
					Console.WriteLine( "{0} After Lock Inconsistency", Thread.CurrentThread.ManagedThreadId );
				}
			};

			var tasks = Enumerable
				.Range( 0, 5 )
				.Select( x => Task.Run( t ) );
			await Task.WhenAll( tasks );

		}
	}
}
