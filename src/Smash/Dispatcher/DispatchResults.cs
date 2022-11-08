namespace Smash.Dispatcher;

/// <summary>Represents a state of message dispatching sequence for <see cref="IMessageDispatcher"/>.</summary>
public enum DispatchResults
{
	/// <summary>The message was successfully dispatched.</summary>
	Succeeded,
	
	/// <summary>The message was not dispatched because an error has occurred.</summary>
	Failed,
	
	/// <summary>The message was not dispatched because it was not handled by any handler.</summary>
	NotMapped,
}