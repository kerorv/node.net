using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Nodes.Net
{
  internal class OutgoingChannel
  {
    private ConcurrentQueue<Message> msgQueue = new ConcurrentQueue<Message>();
    private Socket socket;
    private IPEndPoint host;
    private enum State
    {
      Disconnected,
      Connected,
      Closing,
      Closed,
    }
    private State state = State.Disconnected;
    private byte[] packetHeader = new byte[4];

    internal OutgoingChannel(IPEndPoint remote)
    {
      socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
      host = remote;
      Run();
    }

    internal void Send(Message msg)
    {
      msgQueue.Enqueue(msg);
    }

    private async void Run()
    {
      while (this.state != State.Closed)
      {
        switch (this.state)
        {
          case State.Disconnected:
            {
              await socket.ConnectAsync(host);
              this.state = State.Connected;
            }
            break;
          case State.Connected:
            {
              Message message;
              while (msgQueue.TryDequeue(out message))
              {
                string jsonString = Message.Serialize(message);
                byte[] body = Encoding.ASCII.GetBytes(jsonString);
                byte[] header = BitConverter.GetBytes(body.Length);
                List<ArraySegment<byte>> dataList = new List<ArraySegment<byte>>(){
                  new ArraySegment<byte>(header),
                  new ArraySegment<byte>(body),
                };
                try
                {
                  await this.socket.SendAsync(dataList, SocketFlags.None);
                }
                catch (SocketException e)
                {
                  e.ToString();
                  this.socket.Close();
                  this.state = State.Disconnected;
                  break;
                }
              }

              if (this.state == State.Connected)
              {
                await Task.Delay(100);
              }
            }
            break;
          case State.Closing:
            {
              socket.Close();
              this.state = State.Closed;
            }
            break;
          case State.Closed:
          default:
            break;
        }
      }
    }
  }
}