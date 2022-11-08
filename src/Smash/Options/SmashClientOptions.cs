using System.Net;

namespace Smash.Options;

/// <summary>Represents a configuration for the transport layer.</summary>
public class SmashClientOptions
{
	/// <summary>Gets the ip address.</summary>
	public required string IpAddress { get; set; }
	
	/// <summary>Gets the port.</summary>
	public int Port { get; set; }
	
	/// <summary>Determines whether the client log messages.</summary>
	public bool EnableLogging { get; set; }
	
	internal IPEndPoint GetRemoteEndPoint() =>
		new(IPAddress.Parse(IpAddress), Port);
}