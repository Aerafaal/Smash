using System.Net;

namespace Smash.Options;

/// <summary>Represents a configuration for the transport layer.</summary>
public class SmashServerOptions
{
	/// <summary>Gets the ip address.</summary>
	public required string IpAddress { get; set; }

	/// <summary>Gets the port.</summary>
	public int Port { get; set; }

	/// <summary>Gets the number of max connections accepted by the server.</summary>
	public int MaxConnections { get; set; }
	
	/// <summary>Gets the number of max connections by ip address accepted by the server.</summary>
	public int MaxConnectionsByIpAddress { get; set; }
	
	/// <summary>Determines whether the server log messages.</summary>
	public bool EnableLogging { get; set; }
	
	/// <summary>Determines whether the server use the keep alive feature.</summary>
	public bool EnableKeepAlive { get; set; }
	
	/// <summary>Gets the keep alive interval.</summary>
	public int KeepAliveInterval { get; set; }
	
	internal IPEndPoint GetRemoteEndPoint() =>
		new(IPAddress.Parse(IpAddress), Port);
}