using System.Diagnostics.CodeAnalysis;

namespace Smash.Collections;

/// <summary>Represents a thread-safe collection of <typeparamref name="TSession"/>.</summary>
/// <typeparam name="TSession">The type of the session.</typeparam>
public interface ISessionCollection<TSession>
{
	/// <summary>Gets the number of sessions in the collection.</summary>
	int Count { get; }
	
	/// <summary>Determines whether the collection contains a session.</summary>
	bool Any { get; }
	
	/// <summary>Determines whether the collection is full.</summary>
	bool IsFull { get; }
	
	/// <summary>Gets a session with the specified <paramref name="sessionId"/>.</summary>
	/// <param name="sessionId">The unique identifier of the session.</param>
	/// <returns>The found session.</returns>
	TSession? GetSession(string sessionId);
	
	/// <summary>Removes the session with the specified <paramref name="sessionId"/>.</summary>
	/// <param name="sessionId">The unique identifier of the session.</param>
	void RemoveSession(string sessionId);
	
	/// <summary>Gets a session with the specified <paramref name="predicate"/>.</summary>
	/// <param name="predicate">The predicate that determines whether the session is found.</param>
	/// <returns>The found session.</returns>
	TSession? GetSession(Func<TSession, bool> predicate);
	
	/// <summary>Removes the session with the specified <paramref name="predicate"/>.</summary>
	/// <param name="predicate">The predicate that determines whether the session is found.</param>
	void RemoveSession(Func<TSession, bool> predicate);
	
	/// <summary>Attempts to get a session with the specified <paramref name="sessionId"/>.</summary>
	/// <param name="sessionId">The unique identifier of the session.</param>
	/// <param name="session">The found session.</param>
	/// <returns><see langword="true"/> if the session is found; otherwise, <see langword="false"/>.</returns>
	bool TryGetSession(string sessionId, [NotNullWhen(true)] out TSession? session);
	
	/// <summary>Attempts to remove the session with the specified <paramref name="sessionId"/>.</summary>
	/// <param name="sessionId">The unique identifier of the session.</param>
	/// <param name="session">The found session.</param>
	/// <returns><see langword="true"/> if the session is found; otherwise, <see langword="false"/>.</returns>
	bool TryRemoveSession(string sessionId, [NotNullWhen(true)] out TSession? session);
	
	/// <summary>Attempts to get a session with the specified <paramref name="predicate"/>.</summary>
	/// <param name="predicate">The predicate that determines whether the session is found.</param>
	/// <param name="session">The found session.</param>
	/// <returns><see langword="true"/> if the session is found; otherwise, <see langword="false"/>.</returns>
	bool TryGetSession(Func<TSession, bool> predicate, [NotNullWhen(true)] out TSession? session);
	
	/// <summary>Attempts to remove the session with the specified <paramref name="predicate"/>.</summary>
	/// <param name="predicate">The predicate that determines whether the session is found.</param>
	/// <param name="session">The found session.</param>
	/// <returns><see langword="true"/> if the session is found; otherwise, <see langword="false"/>.</returns>
	bool TryRemoveSession(Func<TSession, bool> predicate, [NotNullWhen(true)] out TSession? session);
	
	/// <summary>Gets the sessions with the specified <paramref name="predicate"/>.</summary>
	/// <param name="predicate">The predicate that determines whether the session is found.</param>
	/// <returns>A collection of session.</returns>
	IEnumerable<TSession> GetSessions(Func<TSession, bool>? predicate = null);
	
	/// <summary>Removes the sessions with the specified <paramref name="predicate"/>.</summary>
	/// <param name="predicate">The predicate that determines whether the session is found.</param>
	void RemoveSessions(Func<TSession, bool>? predicate = null);

	/// <summary>Determines whether the collection contains a session with the specified <paramref name="predicate"/>.</summary>
	/// <param name="predicate">The predicate that determines whether the session is found.</param>
	/// <returns><see langword="true"/> if the collection contains a session with the specified <paramref name="predicate"/>; otherwise, <see langword="false"/>.</returns>
	bool AnySession(Func<TSession, bool> predicate);
	
	/// <summary>Counts the sessions with the specified <paramref name="predicate"/>.</summary>
	/// <param name="predicate">The predicate that determines whether the session is found.</param>
	/// <returns>The number of sessions with the specified <paramref name="predicate"/>.</returns>
	int CountSessions(Func<TSession, bool> predicate);
	
	/// <summary>Executes asynchronously the specified <paramref name="action"/> on each session in the collection.</summary>
	/// <param name="action">The action to execute.</param>
	/// <param name="predicate">The predicate that determines whether the session is found.</param>
	Task ExecuteAsync(Action<TSession> action, Func<TSession, bool>? predicate = null);
	
	/// <summary>Executes asynchronously the specified <paramref name="action"/> on each session in the collection.</summary>
	/// <param name="action">The action to execute.</param>
	/// <param name="predicate">The predicate that determines whether the session is found.</param>
	Task ExecuteAsync(Func<TSession, ValueTask> action, Func<TSession, bool>? predicate = null);
	
	/// <summary>Executes asynchronously the specified <paramref name="action"/> on each session in the collection.</summary>
	/// <param name="action">The action to execute.</param>
	/// <param name="predicate">The predicate that determines whether the session is found.</param>
	Task ExecuteAsync(Func<TSession, Task> action, Func<TSession, bool>? predicate = null);
}