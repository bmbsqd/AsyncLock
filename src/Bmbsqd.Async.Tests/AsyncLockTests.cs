using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Bmbsqd.Async.Tests
{
	[TestFixture]
	public class AsyncLockTests
	{
		[Test, Timeout( 1000 )]
		public async Task Simple()
		{
			var _lock = new AsyncLock();
			using( await _lock ) {
				Console.WriteLine( "Locked" );
			}
		}

		[Test]
		public async Task ExceptionShouldFlow()
		{
			var _lock = new AsyncLock();

			try {
				using( await _lock ) {
					throw new Exception( "Hello World" );
				}
				Assert.Fail( "Should never hit this line of code" );
			}
			catch( Exception e ) {
				Assert.That( e.Message, Is.EqualTo( "Hello World" ) );
				Trace.WriteLine( "Got Exception" );
			}
		}

		[Test, Timeout( 500 ), Ignore( "No, current AsyncLock is NOT reentrant" )]
		public async Task ReentrantAware()
		{
			var _lock = new AsyncLock();

			using( await _lock ) {
				Console.WriteLine( "In lock" );
				using( await _lock ) {
					Console.WriteLine( "Re-entrant lock" );
				}
			}
		}


		[Test, Timeout( 1000 )]
		public async Task LockReleaseLockRelease()
		{
			var _lock = new AsyncLock();
			using( await _lock ) {
				Console.WriteLine( "Locked" );
			}
			using( await _lock ) {
				Console.WriteLine( "Locked again" );
			}

		}

		[Test]
		public async Task ExceptionShouldUnlockTheLock()
		{
			var _lock = new AsyncLock();

			Assert.That( _lock.HasLock, Is.False );
			try {
				using( await _lock ) {
					Assert.That( _lock.HasLock, Is.True );
					throw new Exception();
				}
			}
			catch( Exception e ) { }
			Assert.That( _lock.HasLock, Is.False );
		}
		
		[Test]
		public async Task ProperlyWaitsForReleaseAndCallsBackThruContext()
		{
			var _lock = new AsyncLock();

			var successfullSteps = 0;

			Action<int> next = expected => {
				var oldValue = Interlocked.CompareExchange( ref successfullSteps, expected + 1, expected );
				if( oldValue != expected ) {
					Trace.WriteLine( string.Format( "Expected {0} but was {1}", expected, oldValue ) );
				}
			};


			await Task.Run( async () => {

				var subTask = Task.Run( async () => {
					await Task.Delay( 50 );
					// assumes the lock is taken at this point
					Assert.That( _lock.HasLock, Is.True );

					using( var l = await _lock ) {
						Console.WriteLine( l );
						var properCallback1 = StackHelper.CurrentCallStack.Any( f => f.GetMethod().DeclaringType == typeof( ExecutionContext ) );
						Assert.That( properCallback1, Is.True );
						next( 2 );
					}
				} );

				using( var l = await _lock ) {
					Console.WriteLine( l );
					next( 0 );
					await Task.Delay( 500 );
					next( 1 );
				}

				await subTask;
			} );

			Assert.That( successfullSteps, Is.EqualTo( 3 ) );
		}
	}
}
