using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Smash.Collections;
using Smash.Dispatcher;
using Smash.Framing;
using Smash.Options;

namespace Smash.Transport;

/// <summary>Represents a tcp server that can be used to listen for incoming connections.</summary>
/// <typeparam name="TSession">The type of the session.</typeparam>
/// <typeparam name="TMessage">The type of the message.</typeparam>
public abstract class SmashServer<TSession, TMessage>
	where TMessage : class
	where TSession : SmashSession<TMessage>
{
	private readonly Socket _socket;
	private readonly CancellationTokenSource _cts;
	private readonly SmashServerOptions _options;
	private readonly IMessageDispatcher<TMessage> _messageDispatcher;
	private readonly ILoggerFactory _loggerFactory;
	private readonly IServiceProvider _provider;
	private readonly ILogger _logger;
	private readonly PeriodicTimer _timer;

	/// <summary>Gets the session collection of type <typeparamref name="TSession"/>.</summary>
	public ISessionCollection<TSession> Sessions { get; }

	/// <summary>Initializes a new instance of the <see cref="SmashServer{TSession, TMessage}"/> class.</summary>
	/// <param name="options">The server options.</param>
	/// <param name="messageDispatcher">The message dispatcher.</param>
	/// <param name="loggerFactory">The logger factory.</param>
	/// <param name="provider">The service provider.</param>
	/// <param name="sessions">The session collection.</param>
	protected SmashServer(
		IOptions<SmashServerOptions> options, 
		IMessageDispatcher<TMessage> messageDispatcher, 
		ILoggerFactory loggerFactory,
		IServiceProvider provider,
		ISessionCollection<TSession> sessions)
	{
		_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		_cts = new CancellationTokenSource();
		_options = options.Value;
		_messageDispatcher = messageDispatcher;
		_loggerFactory = loggerFactory;
		_provider = provider;
		_logger = loggerFactory.CreateLogger("Smash.Transport.SmashServer");
		_timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_options.KeepAliveInterval));
		Sessions = sessions;
	}

	/// <summary>Starts the server asynchronously.</summary>
	public async Task StartAsync()
	{
		var endpoint = _options.GetRemoteEndPoint();
		
		try
		{
			_socket.Bind(endpoint);
		}
		catch (SocketException e)
		{
			_logger.LogError(e, "Failed to bind to {EndPoint}", endpoint);
			throw;
		}
		
		_socket.Listen(_options.MaxConnections);
		
		_logger.LogInformation("Listening on {EndPoint}", endpoint);

		if (_options.EnableKeepAlive)
			_ = PingAsync();

		while (!_cts.IsCancellationRequested)
		{
			var sessionSocket = await _socket.AcceptAsync(_cts.Token).ConfigureAwait(false);

			var logger = _loggerFactory.CreateLogger("Smash.Transport.SmashSession");

			var messageParser = _provider.GetRequiredService<IMessageParser<TMessage>>();
			
			var session = CreateSession(sessionSocket, messageParser, _messageDispatcher, logger, _options);

			if (!CanAddSession(session))
			{
				await session.DisposeAsync().ConfigureAwait(false);
				continue;
			}
			
			Sessions.AddSession(session);

			_ = OnSessionConnectedAsync(session)
				.ContinueWith(_ => OnSessionConnectedAsync(session), _cts.Token)
				.Unwrap()
				.ContinueWith(_ => session.ReceiveAsync(), _cts.Token)
				.Unwrap()
				.ContinueWith(_ => OnSessionDisconnectedAsync(session), _cts.Token)
				.Unwrap()
				.ContinueWith(_ => session.DisposeAsync().AsTask(), _cts.Token)
				.Unwrap()
				.ContinueWith(_ => Sessions.RemoveSession(session.SessionId), _cts.Token)
				.ConfigureAwait(false);
		}
	}

	/// <summary>Initializes a new instance of the <see cref="TSession"/> class.</summary>
	/// <param name="socket">The bound socket.</param>
	/// <param name="messageParser">The message parser.</param>
	/// <param name="messageDispatcher">The message dispatcher.</param>
	/// <param name="logger">The logger.</param>
	/// <param name="options">The server options.</param>
	protected abstract TSession CreateSession(
		Socket socket,
		IMessageParser<TMessage> messageParser,
		IMessageDispatcher<TMessage> messageDispatcher,
		ILogger logger,
		SmashServerOptions options);
	
	private async Task PingAsync()
	{
		while (await _timer.WaitForNextTickAsync(_cts.Token).ConfigureAwait(false))
		{
			if (_cts.IsCancellationRequested)
				return;

			await Sessions.ExecuteAsync(x => x.DisposeAsync(), x => !x.IsConnected).ConfigureAwait(false);
		}
	}

	/// <summary>Determines whether the session can be added to the session collection.</summary>
	/// <param name="session">The session to add.</param>
	/// <returns><see langword="true" /> if the session can be added; otherwise, <see langword="false" />.</returns>
	protected virtual bool CanAddSession(TSession session) =>
		!Sessions.IsFull && 
		Sessions.CountSessions(x => x.RemoteEndPoint.Address.ToString().Equals(session.RemoteEndPoint.Address.ToString(), StringComparison.InvariantCultureIgnoreCase)) < _options.MaxConnectionsByIpAddress;

	/// <summary>Called when a session is connected.</summary>
	/// <param name="session">The connected session.</param>
	protected virtual Task OnSessionConnectedAsync(TSession session)
	{
		_logger.LogInformation("Session ({Name}) connected from {EndPoint}", session, session.RemoteEndPoint);
		return Task.CompletedTask;
	}

	/// <summary>Called when a session is disconnected.</summary>
	/// <param name="session">The session will be disconnected.</param>
	protected virtual Task OnSessionDisconnectedAsync(TSession session)
	{
		_logger.LogInformation("Session ({Name}) disconnected from {EndPoint}", session, session.RemoteEndPoint);
		return Task.CompletedTask;
	}
}