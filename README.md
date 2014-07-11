AsyncLock
=========

Async/Awaitable light, non-blocking lock in C#


## Usage ##

First install the nuget package https://www.nuget.org/packages/Bmbsqd.AsyncLock/


To create a new lock, you have to new it up
```csharp
private AsyncLock _lock = new AsyncLock();
```

And to protect a section of code using the lock 
```csharp
using( await _lock ) {
	_log.Info( "Inside Lock" );
}
```

## Why? ##
Lightweight, fast, no tasks involved in the await process, very little overhead, `Interlocked`.

Internally uses `ConcurrentQueue` to hold waiters, but will bypass structure completely if there's nothing to wait for.

## Alternatives ##
 - Nito AsyncEx -- http://nitoasyncex.codeplex.com/
 - W8 and WP8 -- http://asynclock.codeplex.com/
 - [System.Threading.SemaphoreSlim](http://msdn.microsoft.com/en-us/library/system.threading.semaphoreslim(v=vs.110).aspx) 

## Gotchas ##
AsyncLock is not reentrant and will deadlock its self when entering the same lock multiple times on same execution path

There's no deadlock monitoring built in.

## It's fast and lightweight ##
Best case
  - 1x Interlocked on enter
  - 1x Interlocked on exit

Worst case:
  - 3x Interlocked on enter
  - 1x Interlocked on exit
  - ConcurrentQueue Enqueue/TryDequeue calls

## Future plans ##
  - Move `GetAwaiter()` result to a struct and keep lock reference and potential heavier `Waiter` class references in the struct

