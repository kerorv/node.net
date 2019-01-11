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
    private ConcurrentDictionary<IPEndPoint, Lazy<OutgoingChannel>> outChannels =
      new ConcurrentDictionary<IPEndPoint, Lazy<OutgoingChannel>>();
    private List<IncomingChannel> inChannels = new List<IncomingChannel>();

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

      while (true)
      {
        try
        {
          Socket clientSocket = await this.serverSocket.AcceptAsync();
          IncomingChannel channel = new IncomingChannel(clientSocket);
          inChannels.Add(channel);
          channel.Start();
        }
        catch (Exception e)
        {
          Console.WriteLine("Hub::Run exception: {0}", e.Message);
          return;
        }
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