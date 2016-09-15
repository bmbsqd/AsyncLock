using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Bmbsqd.Async.Tests {
	public class AsyncLockTests {
		private readonly ITestOutputHelper _output;

		public AsyncLockTests( ITestOutputHelper output )
		{
			_output = output;
		}

		[Fact]
		public async Task Simple()
		{
			var _lock = new AsyncLock();
			using( await _lock ) {
				Console.WriteLine( "Locked" );
			}
		}

		public class ExpectedException : Exception {
			public ExpectedException() { }
			public ExpectedException( string message ) : base( message ) { }
		}


		[Fact]
		public async Task ExceptionShouldFlow()
		{
			var _lock = new AsyncLock();

			try {
				using( await _lock ) {
					throw new ExpectedException( "Hello World" );
				}
				throw new Exception( "Should never hit this line of code" );
			}
			catch( ExpectedException e ) {
				Assert.Equal( "Hello World", e.Message );
			}
		}

		[Fact( Skip = "No, current AsyncLock is NOT reentrant" )]
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


		[Fact]
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

		[Fact]
		public async Task ExceptionShouldUnlockTheLock()
		{
			var _lock = new AsyncLock();

			Assert.False( _lock.HasLock );
			try {
				using( await _lock ) {
					Assert.True( _lock.HasLock );
					throw new ExpectedException();
				}
			}
			catch( ExpectedException ) { }
			Assert.False( _lock.HasLock );
		}

		[Fact]
		public async Task ProperlyWaitsForReleaseAndCallsBackThruContext()
		{
			var _lock = new AsyncLock();

			var successfullSteps = 0;

			Action<int> next = expected => {
				var oldValue = Interlocked.CompareExchange( ref successfullSteps, expected + 1, expected );
				if( oldValue != expected ) {
					_output.WriteLine( "Expected {0} but was {1}", expected, oldValue );
				}
			};


			await Task.Run( async () => {
				var subTask = Task.Run( async () => {
					await Task.Delay( 50 );
					// assumes the lock is taken at this point
					Assert.True( _lock.HasLock );

					using( var l = await _lock ) {
						Console.WriteLine( l );
						Assert.True( StackHelper.Text.Contains( "at System.Threading.ExecutionContext." ) ); // TODO: Until we have stacktrace / stack frame support
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
			}, CancellationToken.None );

			Assert.Equal( 3, successfullSteps );
		}
	}
}
