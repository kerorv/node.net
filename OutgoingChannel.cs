using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Nodes.Net
{
  internal class OutgoingChannel
  {
    private ConcurrentQueue<Message> msgQueue = new ConcurrentQueue<Message>();
    private TaskCompletionSource<object> completeSource = new TaskCompletionSource<object>();
    private object mutex = new object();
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
      lock (mutex)
      {
        completeSource.TrySetResult(null);
      }
    }

    private Task WaitAsync()
    {
      lock (mutex)
      {
        return completeSource.Task;
      }
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
              try
              {
                await WaitAsync();
              }
              catch
              {
                break;
              }

              Message message;
              while (this.state == State.Connected && msgQueue.TryDequeue(out message))
              {
                string jsonString = Message.Serialize(message);
                byte[] body = Encoding.ASCII.GetBytes(jsonString);
                byte[] header = BitConverter.GetBytes(body.Length);
                List<ArraySegment<byte>> dataList = new List<ArraySegment<byte>>()
                  {
                    new ArraySegment<byte>(header),
                    new ArraySegment<byte>(body),
                  };

                try
                {
                  int sendBytes = await this.socket.SendAsync(dataList, SocketFlags.None);
                }
                catch (Exception e)
                {
                  Console.WriteLine("OutgoingChannel::Run exception: {0}", e.ToString());
                  break;
                }
              }

              lock (mutex)
              {
                completeSource = new TaskCompletionSource<object>();
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

    public void Close()
    {
      msgQueue.Clear();

      lock (mutex)
      {
        completeSource.TrySetCanceled();
      }

      this.state = State.Closing;
    }
  }
}