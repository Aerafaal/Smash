using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Options;
using Smash.Options;
using Smash.Transport;

namespace Smash.Collections;

/// <inheritdoc />
public class SessionCollection<TSession, TMessage> : ISessionCollection<TSession>
	where TMessage : class
	where TSession : SmashSession<TMessage>
{
	protected SmashServerOptions Options { get; }
	
	protected ConcurrentDictionary<string, TSession> Sessions { get; }

	/// <inheritdoc />
	public int Count =>
		Sessions.Count;

	/// <inheritdoc />
	public bool Any =>
		Sessions.Count > 0;

	/// <inheritdoc />
	public bool IsFull =>
		Options.MaxConnections > 0 && Count >= Options.MaxConnections;
	
	public SessionCollection(IOptions<SmashServerOptions> options)
	{
		Options = options.Value;
		Sessions = new ConcurrentDictionary<string, TSession>(StringComparer.InvariantCultureIgnoreCase);
	}

	/// <inheritdoc />
	public TSession? GetSession(string sessionId) =>
		Sessions.TryGetValue(sessionId, out var session) 
			? session 
			: null;

	/// <inheritdoc />
	public void RemoveSession(string sessionId) =>
		Sessions.TryRemove(sessionId, out _);

	/// <inheritdoc />
	public TSession? GetSession(Func<TSession, bool> predicate) =>
		Sessions.Values.FirstOrDefault(predicate);

	/// <inheritdoc />
	public void RemoveSession(Func<TSession, bool> predicate)
	{
		var session = Sessions.Values.FirstOrDefault(predicate);
		
		if (session is null)
			return;
		
		Sessions.TryRemove(session.SessionId, out _);
	}

	/// <inheritdoc />
	public bool TryGetSession(string sessionId, [NotNullWhen(true)] out TSession? session) =>
		Sessions.TryGetValue(sessionId, out session);

	/// <inheritdoc />
	public bool TryRemoveSession(string sessionId, [NotNullWhen(true)] out TSession? session) =>
		Sessions.TryRemove(sessionId, out session);

	/// <inheritdoc />
	public bool TryGetSession(Func<TSession, bool> predicate, [NotNullWhen(true)] out TSession? session) =>
		(session = Sessions.Values.FirstOrDefault(predicate)) is not null;

	/// <inheritdoc />
	public bool TryRemoveSession(Func<TSession, bool> predicate, [NotNullWhen(true)] out TSession? session) =>
		(session = Sessions.Values.FirstOrDefault(predicate)) is not null && Sessions.TryRemove(session.SessionId, out _);

	/// <inheritdoc />
	public IEnumerable<TSession> GetSessions(Func<TSession, bool>? predicate = null) =>
		predicate is null 
			? Sessions.Values 
			: Sessions.Values.Where(predicate);

	/// <inheritdoc />
	public void RemoveSessions(Func<TSession, bool>? predicate = null)
	{
		if (predicate is null)
		{
			Sessions.Clear();
			return;
		}

		foreach (var session in Sessions.Values.Where(predicate))
			Sessions.TryRemove(session.SessionId, out _);
	}

	/// <inheritdoc />
	public bool AnySession(Func<TSession, bool> predicate) =>
		Sessions.Values.Any(predicate);

	/// <inheritdoc />
	public int CountSessions(Func<TSession, bool> predicate) =>
		Sessions.Values.Count(predicate);

	/// <inheritdoc />
	public Task ExecuteAsync(Action<TSession> action, Func<TSession, bool>? predicate = null)
	{
		var sessions = predicate is null 
			? Sessions.Values 
			: Sessions.Values.Where(predicate);

		return Task.WhenAll(sessions.Select(session => Task.Run(() => action(session))));
	}

	/// <inheritdoc />
	public Task ExecuteAsync(Func<TSession, ValueTask> action, Func<TSession, bool>? predicate = null)
	{
		var sessions = predicate is null 
			? Sessions.Values 
			: Sessions.Values.Where(predicate);
		
		return Task.WhenAll(sessions.Select(session => action(session).AsTask()));
	}

	/// <inheritdoc />
	public Task ExecuteAsync(Func<TSession, Task> action, Func<TSession, bool>? predicate = null)
	{
		var sessions = predicate is null 
			? Sessions.Values 
			: Sessions.Values.Where(predicate);
		
		return Task.WhenAll(sessions.Select(action));
	}
}