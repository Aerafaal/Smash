namespace Smash.Framing;

/// <summary>Describes a way to encode a network message.</summary>
/// <typeparam name="TMessage">The type of the message.</typeparam>
public interface IMessageEncoder<in TMessage>
	where TMessage : class
{
	/// <summary>Encodes a network message.</summary>
	/// <param name="message">The message to encode.</param>
	/// <param name="isWrittenByServer">Whether the message is written by a server session.</param>
	/// <returns>A encoded message as an <see cref="ReadOnlyMemory{T}"/>.</returns>
	ReadOnlyMemory<byte> EncodeMessage(TMessage message, bool isWrittenByServer);
}