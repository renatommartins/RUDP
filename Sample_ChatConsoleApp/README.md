# Sample Chat Console Application

Simple chat using the RUDP protocol for network communication.

There are two modes of use:

**Client Mode**

To start this mode use the following parameters:
	Sample_ChatConsoleApp -c name appid address:port
	e.g.: Sample_ChatConsoleApp -c MyUserNickname 43707 127.0.0.1:1337

**Server Mode**

To start this mode use the following parameters:
	Sample_ChatConsoleApp -s appid bindaddress:port
	e.g.: Sample_ChatConsoleApp -s 43707 0.0.0.0:1337
