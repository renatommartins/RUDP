using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace RUDP
{
    internal enum EventType
    {
        ConnectionRequest,
        ConnectionAccept,
        DisconnectionNotice,
        DataReceive,
    }

    internal struct Event
    {
        public EventType type;
        public object args;
    }

    internal interface IEventListener
    {
        IPEndPoint remoteEndPoint{ get; }
        EventType[] EventMask { get; }
        void NextEvent(Event e);
        
    }

    internal struct RudpFrameRaw
    {
        public ushort protocolKey;
        public ushort sequenceNumber;
        public ushort lastReceivedSequence;

    }

    internal class OpenSocket
    {
        public Socket Socket { get; set; }
        public bool IsActive { get; set; }

        public OpenSocket(Socket socket)
        {
            this.Socket = socket;
            IsActive = true;
        }
    }

    internal static class UdpSocketManager
    {
        private static Dictionary<IPEndPoint, IEventListener> _listeningSockets;
        private static List<OpenSocket> _openSockets;

        static UdpSocketManager()
        {
            _listeningSockets = new Dictionary<IPEndPoint, IEventListener>();
            _openSockets = new List<OpenSocket>();
        }

        public static Unsubscriber<IPEndPoint, IEventListener> Listen(IEventListener listener, IPEndPoint endPoint)
        {
            if(!_listeningSockets.ContainsKey(endPoint))
            {
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                socket.Bind(endPoint);

                _listeningSockets.Add(endPoint, listener);
                _openSockets.Add(new OpenSocket(socket));

                return new Unsubscriber<IPEndPoint, IEventListener>(_listeningSockets, listener);
            }
            else
            {
                throw new Exception("endpoint is already listeining");
            }
        }

        private static void SocketThread()
        {
            byte[] receiveBuffer = new byte[32 * 1024];

            while (true)
            {
                foreach(OpenSocket openSocket in _openSockets)
                {
                    while (openSocket.Socket.Available > 0)
                    {
                        EndPoint endPoint = new IPEndPoint(IPAddress.Any, 0);
                        openSocket.Socket.ReceiveFrom(receiveBuffer, 0, receiveBuffer.Length, SocketFlags.None, ref endPoint);

                        if (_listeningSockets.ContainsKey((IPEndPoint)endPoint))
                        {

                        }
                    }
                }
                
                Thread.Yield();
            }
        }
    }

    public class RudpListener
    {
        private static List<Socket> _openSockets;

        private void listenThread()
        {

        }
    }

	public class Unsubscriber<R,T> : IDisposable
    {
        private Dictionary<R, T> observers;
        private T observer;

        public Unsubscriber(Dictionary<R, T> observers, T observer)
        {
            this.observers = observers;
            this.observer = observer;
        }

        public void Dispose()
        {
            if (observers != null && observers.ContainsValue(observer))
            {
                R key = default;

                foreach(KeyValuePair<R, T> pair in observers)
                {
                    key = pair.Key;
                }

                observers.Remove(key);
            }
        }
    }
}
