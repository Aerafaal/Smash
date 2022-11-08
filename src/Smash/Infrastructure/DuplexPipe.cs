using System.IO.Pipelines;
using System.Net.Sockets;

namespace Smash.Infrastructure;

/// <inheritdoc />
public sealed record DuplexPipe(PipeReader Input, PipeWriter Output) : IDuplexPipe
{
	/// <summary>Creates a new <see cref="IDuplexPipe"/> wrapped from the specified <paramref name="socket"/>.</summary>
	/// <param name="socket">The wrapped socket.</param>
	/// <returns>A <see cref="IDuplexPipe"/> that wrap the <paramref name="socket"/>.</returns>
	public static IDuplexPipe Create(Socket socket)
	{
		var ns = new NetworkStream(socket);
		
		return new DuplexPipe(PipeReader.Create(ns), PipeWriter.Create(ns));
	}
}