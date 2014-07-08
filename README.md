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


If you don't need the continuation to retain the execution context then use the `WithoutContext` property
```csharp
using( await _lock.WithoutContext ) {
	_log.Info( "Inside Lock, may or may not be in same execution context" );
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
AsyncLock is not reentrant and will fail deadlock its self when entering the same lock multiple times on same execution path

There's no deadlock monitoring built in.
