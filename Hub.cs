using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Nodes.Net
{
  internal delegate void DispatchRemoteMessageDelegate(IPEndPoint from, Message message);
  internal class Hub
  {
    private IPEndPoint serverEP;
    private Socket serverSocket;
    private ConcurrentDictionary<IPEndPoint, Lazy<OutgoingChannel>> outChannels =
      new ConcurrentDictionary<IPEndPoint, Lazy<OutgoingChannel>>();
    private List<IncomingChannel> inChannels = new List<IncomingChannel>();
    private DispatchRemoteMessageDelegate dispatcher;
    public int MaxOutChannelNum { get; set; } = 100;

    internal Hub(IPEndPoint host, DispatchRemoteMessageDelegate drmDelegate)
    {
      serverEP = host;
      dispatcher = drmDelegate;
    }

    internal Hub(DispatchRemoteMessageDelegate drmDelegate)
      : this(new IPEndPoint(IPAddress.Any, 10021), drmDelegate)
    {
    }

    internal void Start()
    {
      this.serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
      this.serverSocket.Bind(serverEP);
      this.serverSocket.Listen(32);

      Run();
    }

    internal void Close()
    {
      this.serverSocket.Close();

      foreach (IncomingChannel channel in inChannels)
      {
        channel.Close();
      }

      foreach (var lazyChannel in outChannels.Values)
      {
        var outChanel = lazyChannel.Value;
        outChanel.Close();
      }
    }

    private async void Run()
    {
      try
      {
        while (true)
        {

          Socket clientSocket = await this.serverSocket.AcceptAsync();
          IncomingChannel channel = new IncomingChannel(clientSocket, this.dispatcher);
          inChannels.Add(channel);
          channel.Start();
        }
      }
      catch (Exception e)
      {
        Console.WriteLine("Hub::Run exception: {1} {0}", e.Message, e.GetType().ToString());
      }
    }

    internal void Send(IPEndPoint address, Message msg)
    {
      var lazyChannel = outChannels.GetOrAdd(
        address, key => new Lazy<OutgoingChannel>(
          () => new OutgoingChannel(key)));
      var channel = lazyChannel.Value;
      channel.Send(msg);
    }
  }
}