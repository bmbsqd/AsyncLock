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
Lightweight, fast, no tasks involved in the await process