namespace Smash.Framing;

/// <summary>Describes a way to parse a network message.</summary>
/// <typeparam name="TMessage">The type of the message.</typeparam>
public interface IMessageParser<TMessage> : IMessageDecoder<TMessage>, IMessageEncoder<TMessage> where TMessage : class { }