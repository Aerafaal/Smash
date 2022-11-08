using System.Diagnostics.CodeAnalysis;

namespace Smash.Factory;

/// <summary>A factory abstraction that creates instances of <typeparamref name="TMessage"/> based on its <typeparamref name="TKey"/>.</summary>
/// <typeparam name="TKey">The type of the key.</typeparam>
/// <typeparam name="TMessage">The type of the message.</typeparam>
public interface IMessageFactory<in TKey, TMessage>
	where TKey : notnull
	where TMessage : class
{
	/// <summary>Attempts to get a <paramref name="message"/> based on its <paramref name="key"/>.</summary>
	/// <param name="key">The key of the message.</param>
	/// <param name="message">The found message.</param>
	/// <returns><see langword="true"/> if the message was found; otherwise, <see langword="false"/>.</returns>
	bool TryGetMessage(TKey key, [NotNullWhen(true)] out TMessage? message);
}