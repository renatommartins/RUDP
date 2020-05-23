#RUDP

This network protocol is based on the idea of a reliable communication on top of a UDP socket.

It is meant for applications that need a realtime communication and the ability to control its own reliability, games are a prime example of this use case.

## Summary
* [Goals](#goals)
* [Limitations](#limitations)
* [Possible Improvements](#possible-improvements)
* [Credits](#credits)
* [How it Works](#how-it-works)
  * [Header Structure](#header-structure)
* [Usage](#usage)
  * [Server-side](#server-side)
  * [Client-side](#client-side)

## Goals

* Realtime Communication, have the lowest delay possible in delivering data to user application. this impedes the use of TCP as it may decide to delay data sending until it is able to form a larger packet or in case of issues, like out-of-order delivery or packet dropping, hold received data from the user application until it synchronizes back with the remote side. This forces the use of UDP;
* Data Loss, UDP is a connection-less protocol and all it does is send a packet to an address and forget about it. If data is lost, it is lost for good and both sides have no way of noticing this issue. A virtual connection is needed to keep track of the communication;
* Threading Flexibility, This is a middleware, it is important that it doesn't force architectural decisions on the user application and so this implementation runs on its own thread and interactions with the user application are provided through thread-safe calls.
* Ease of use, Since the problems this protocol aims to solve is so similar to TCP, the API is similar to dotnet's TcpClient and TcpListener classes so it has a familiar use. However it does behave differently in a few ways.

## Limitations
* Doesn't guarantee delivery, it only notifies the user application if delivery succeded or didn't. It doesn't attempt to retry sending the lost data, that is up to the user application.
* Doesn't deliver data in the order it was sent, data may reach the remote side out of order and is immediatelly delivered to user application regardless of the order it was sent.
* Doesn't provide security, in the future the client may allow replacing the medium it sends and receives data through, this would allow for a DTLS connection to be used and provide security. There won't be an attempt on providing security since it isn't simple to develop security protocols, I strongly recommend that a robust solution is used for that end, eg.: OpenSSL.

## Possible Improvements
* Allow replacing the socket class used by the client and listener.
* Add test cases to better validate functionality.
* Add quality of service wrapper class that has reliability and data sequence ordering built-in.
* Better handshaking process, adding retries for example.
* Change update rate while connected.
* Assimetrical update rate.
* Allow stopping listening without terminating the connections.
* Better multi-threading for server-side.

## Credits
Glenn Fiedler for his articles (https://gafferongames.com/) which this protocol was based on.

## How it Works
The clients form a virtual connection over UDP by adding a control header to sent data.
Communication is kept at a fixed update rate defined by the server on handshake, a keep alive packet is sent when there is no user data to send.
The packet header contains information that tracks the last 33 packets acknowledge status.
If no packet is received in the time that it takes to send 32 packets, the connection is considered dropped and a disconnection packet is sent just in case. this effectively puts a limit on how much latency is allowed by the update rate, an update rate of 100Hz would allow at most 320ms of latency (maximum latency is defined by the period times 32).
The implementation is entirely non-blocking, it runs on its own thread.

### Header Structure
```
| Application ID..............(16 bits) |
| Sequence Number.............(16 bits) |
| Acknowledge Sequence Number.(16 bits) |
| Acknowledge Bitfield........(32 bits) |
| Packet Type.................(16 bits) |
| Data.....................(0 - n bits) |
| CRC-32 .....................(32 bits) |
```

* Application ID (16 bits): an arbitrary number used by the client to validate that the applications communicating match each other. This could be used to prevent mismatched version applications connecting for example.
* Sequence Number (16 bits): works like an ID for the message, so the remote side can keep track of what arrived or didn't and report back.
* Acknowledge Sequence Number (16 bits): the last highest Sequence Number number received from the remote side, this is part of the report to the remote so it can figure out what succeded or didn't.
* Acknowledge bitfield (32 bits): reports the status of the 32 packets received before the last sequence number, second part of reporting packet reception to remote.
* Packet Type (16 bits): determines the packet intention, they are the following: Connection Request, sent by a client when it wants to connect to a server; Connection Accept, reply to connection request sent by the server, the data field for this packet contains the protocol version and server update rate; Connection Refuse, reply to connection request when the request is invalid; Disconnection Notify, used to indicate the remote that the connection was terminated; Keep Alive, used to maintain the reports and check that the connection is still alive when there is no data being sent by the user application; Data, indicates that there is user application data carried by the packet.
* Data (n bits): this part may be or not present depending on packet type. This is mostly used by the Data type to carry user data, but Connection Accept packet type uses this part to inform of connection parameters like the version and update rate for example.
* CRC-32 (32 bits): used to validate packet integrity, it gets discarded if the check fails and counts as lost packet.

## Usage

### Server-side

```cs
// Create instance defining Application ID, local address and Update Rate in constructor parameters.
RudpListener listener = new RudpListener(0x0001, new IPEndPoint(IPAddress.Any, 42), 100);

// Start listening to connection requests.
listener.Start();

// Check for connection requests.
if (listener.Pending())
	// Accept new connection, this is made in a queue style so first to request is the first to be accept.
	RudpClient client = listener.AcceptClient();

// Stopping the listener also terminates all connections that it accepted.
listener.Stop();
```

### Client-side

```cs
// Create instance defining the Application ID.
RudpClient client = new RudpClient(appId);

// Start connection attempt to server, this is a non blocking call.
client.Connect(new IPEndPoint(new IPAddress(new byte[]{ 127, 0, 0, 1 }), 42));

// Check if handshake is over and successful.
if (!client.IsConnecting && client.Connected)
{
	// Send data and optionally record the packet sequence number the data will be sent in.
	// Multiple calls may send data in the same packet, the user has to separate the data in that case.
	ushort packetSeqNum = client.Send(new byte[]{ 1, 2, 3, 4, 5, 6, 7, 8 });
	
	// Check if data was received.
	if (client.Available > 0)
		// Received data is kept in a queue so this call will always retrieve the oldest data received.
		// This may be used in a while loop to retrieve any data that was received.
		byte[] receivedData = client.Receive();
	
	// Check the acknowledge state of sent data, use the recorded sequence number returned from the 'Send' call
	// to relate the sequence number to the data you sent.
	List<(ushort, RudpEvent)> AcknowledgeList = client.GetPacketResults();
	foreach((ushort seqNum, RudpEvent rudpEvent) result in AcknowledgeList)
		if(result.rudpEvent == RudpEvent.Successful)
			// Packet was successfully delivered.
			Console.WriteLine($"Packet {result.seqNum} was successfully delivered");
		else if(result.rudpEvent == RudpEvent.Dropped)
			// Packet was dropped.
			Console.WriteLine($"Packet {result.seqNum} was dropped");
		else if(result.rudpEvent == RudpEvent.Pending)
			// Packet has not been acknowledged yet.
			Console.WriteLine($"Packet {result.seqNum} is pending acknowledge");
	
	// Clear the acknowledge result list.
	client.ClearPacketResults();
}
```
