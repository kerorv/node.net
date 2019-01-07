using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Nodes.Net
{
  internal class Hub
  {
    private IPEndPoint serverEP = new IPEndPoint(IPAddress.Any, 10021);
    private Socket serverSocket;
    private CancellationTokenSource cts = new CancellationTokenSource();
    private ConcurrentDictionary<IPEndPoint, Lazy<OutgoingChannel>> outChannels;

    internal void Start()
    {
      this.serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
      this.serverSocket.Bind(serverEP);
      this.serverSocket.Listen(32);
      Run();
    }

    private async void Run()
    {
      Socket clientSocket = await this.serverSocket.AcceptAsync();
      IncomingChannel channel = new IncomingChannel(clientSocket);
      channel.Start();
    }

    internal void Send(Message msg)
    {
      var lazyChannel = outChannels.GetOrAdd(msg.to.address, key => new Lazy<OutgoingChannel>(()=>new OutgoingChannel(key)));
      var channel = lazyChannel.Value;
      channel.Send(msg);
    }
  }
}