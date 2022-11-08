using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Smash.Dispatcher;
using Smash.Framing;
using Smash.Infrastructure;
using Smash.Options;

#pragma warning disable MA0015 // 'dispatchResult' is not a valid parameter name

namespace Smash.Transport;

/// <summary>A network client that represents a connection to a endpoint.</summary>
/// <typeparam name="TMessage">The type of the message.</typeparam>
public abstract class SmashClient<TMessage> : IAsyncDisposable
	where TMessage : class
{
	private readonly Socket _socket;
	private readonly CancellationTokenSource _cts;
	private readonly IDuplexPipe _pipe;
	private readonly IMessageDecoder<TMessage> _messageDecoder;
	private readonly IMessageEncoder<TMessage> _messageEncoder;
	private readonly ILogger _logger;
	private readonly SmashClientOptions _options;

	private bool _disposed;
	private string? _sessionId;

	/// <summary>Gets the unique identifier of the underlying client.</summary>
	public string SessionId =>
		_sessionId ??= UuidGenerator.NewGuid();
	
	/// <summary>Gets the remote endpoint of the underlying client.</summary>
	public IPEndPoint RemoteEndPoint =>
		(IPEndPoint)_socket.RemoteEndPoint!;

	/// <summary>Triggered when the session is closed.</summary>
	public CancellationToken SessionClosed =>
		_cts.Token;
	
	/// <summary>Initializes a new instance of the <see cref="SmashSession{TMessage}"/> class.</summary>
	/// <param name="messageParser">The message parser.</param>
	/// <param name="logger">The logger.</param>
	/// <param name="options">The server options.</param>
	protected SmashClient(IMessageParser<TMessage> messageParser, ILogger logger, IOptions<SmashClientOptions> options)
	{
		_socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		_messageDecoder = messageParser;
		_messageEncoder = messageParser;
		_cts = new CancellationTokenSource();
		_pipe = DuplexPipe.Create(_socket);
		_logger = logger;
		_options = options.Value;
	}

	public virtual async Task ConnectAsync()
	{
		try
		{
			await _socket.ConnectAsync(_options.GetRemoteEndPoint(), _cts.Token).ConfigureAwait(false);
		}
		catch (SocketException e)
		{
			_logger.LogError("Failed to connect to server: {Message}", e.Message);
		}
		
		_logger.LogInformation("Connected to server: {RemoteEndPoint}", RemoteEndPoint);

		await ReceiveAsync().ConfigureAwait(false);
	}

	protected virtual async Task ReceiveAsync()
	{
		await OnConnectedAsync().ConfigureAwait(false);

		try
		{
			while (!_cts.IsCancellationRequested)
			{
				var readResult = await _pipe.Input.ReadAsync(_cts.Token).ConfigureAwait(false);

				if (readResult.IsCanceled)
					break;

				var buffer = readResult.Buffer;

				try
				{
					foreach (var message in _messageDecoder.DecodeMessages(buffer, false))
					{
						var dispatchResult = await OnReceiveMessageAsync(message).ConfigureAwait(false);

						if (!_options.EnableLogging || !_logger.IsEnabled(LogLevel.Debug))
							continue;

						// ReSharper disable once TemplateIsNotCompileTimeConstantProblem
						_logger.LogDebug(dispatchResult switch
						{
							DispatchResults.Succeeded => "Client ({Name}) dispatched message ({Message}) successfully",
							DispatchResults.Failed => "Client ({Name}) failed to dispatch message ({Message})",
							DispatchResults.NotMapped => "Client ({Name}) failed to dispatch message ({Message}) because it is not mapped",
							_ => throw new ArgumentOutOfRangeException(nameof(dispatchResult), dispatchResult, null)
						}, ToString(), message);
					}

					if (readResult.IsCompleted)
					{
						if (!buffer.IsEmpty)
							throw new InvalidOperationException("Incomplete message received");

						break;
					}
				}
				finally
				{
					_pipe.Input.AdvanceTo(buffer.Start, buffer.End);
				}
			}
		}
		catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException)
		{
			/* ignore */
		}
		finally
		{
			await OnDisconnectedAsync().ConfigureAwait(false);
		}
	}

	/// <summary>Asynchronously sends a message to the remote endpoint.</summary>
	/// <param name="message">The message to send.</param>
	public ValueTask SendAsync(TMessage message)
	{
		if (_disposed)
			throw new ObjectDisposedException(nameof(SmashSession<TMessage>));

		if (_cts.IsCancellationRequested)
			return ValueTask.CompletedTask;

		var buffer = _messageEncoder.EncodeMessage(message, false);

		var flushTask = buffer.IsEmpty
			? _pipe.Output.FlushAsync(_cts.Token)
			: _pipe.Output.WriteAsync(buffer, _cts.Token);

		return !flushTask.IsCompletedSuccessfully
			? FireAndForget(flushTask)
			: ValueTask.CompletedTask;
		
		static async ValueTask FireAndForget(ValueTask<FlushResult> flushTask) =>
			await flushTask.ConfigureAwait(false);
	}

	/// <summary>Disconnect the session from the remote endpoint.</summary>
	/// <param name="delay">The delay before the session is disconnected.</param>
	public void Disconnect(TimeSpan? delay = null)
	{
		if (_cts.IsCancellationRequested)
			return;
		
		if (delay.HasValue)
			_cts.CancelAfter(delay.Value);
		else
			_cts.Cancel();
		
		_pipe.Input.CancelPendingRead();
		_pipe.Output.CancelPendingFlush();
	}

	/// <inheritdoc />
	public async ValueTask DisposeAsync()
	{
		if (_disposed)
			return;
		
		_disposed = true;
		
		Disconnect();

		await _pipe.Input.CompleteAsync().ConfigureAwait(false);
		await _pipe.Output.CompleteAsync().ConfigureAwait(false);
		
		try
		{
			_socket.Shutdown(SocketShutdown.Both);
		}
		catch (SocketException)
		{
			/* ignore */
		}
		
		_socket.Close();
		_socket.Dispose();
		_cts.Dispose();
		
		GC.SuppressFinalize(this);
	}

	protected abstract ValueTask OnConnectedAsync();
	
	protected abstract ValueTask OnDisconnectedAsync();

	protected abstract ValueTask<DispatchResults> OnReceiveMessageAsync(TMessage message);
}