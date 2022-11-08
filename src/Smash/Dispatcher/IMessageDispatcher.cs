using Smash.Transport;

namespace Smash.Dispatcher;

/// <summary>Represents a dispatcher that can be used to invoke asynchronous message delegates.</summary>
/// <typeparam name="TMessage">The type of the message.</typeparam>
public interface IMessageDispatcher<TMessage>
	where TMessage : class
{
	/// <summary>Asynchronously invokes the specified message delegate.</summary>
	/// <param name="session">The session that is associated with the message.</param>
	/// <param name="message">The message that is being dispatched.</param>
	/// <returns>A result that represents the dispatch operation.</returns>
	Task<DispatchResults> DispatchAsync(SmashSession<TMessage> session, TMessage message);
}