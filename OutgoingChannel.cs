using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nodes.Net
{
  internal class OutgoingChannel
  {
    private ConcurrentQueue<Message> msgQueue = new ConcurrentQueue<Message>();
    private TaskCompletionSource<object> completeSource = new TaskCompletionSource<object>();
    private CancellationTokenSource cancelSource = new CancellationTokenSource();
    private object mutexWaitQueue = new object();
    private Socket socket;
    private IPEndPoint host;
    private const int StateDisconnected = 0;
    private const int StateConnected = 1;
    private const int StateClosing = 2;
    private const int StateClosed = 3;
    private volatile int state = StateDisconnected;
    private byte[] packetHeader = new byte[4];

    internal OutgoingChannel(IPEndPoint remote)
    {
      host = remote;
      Run();
    }

    internal void Send(Message msg)
    {
      msgQueue.Enqueue(msg);
      lock (mutexWaitQueue)
      {
        completeSource.TrySetResult(null);
      }
    }

    internal void Close()
    {
      msgQueue.Clear();

      lock (mutexWaitQueue)
      {
        completeSource.TrySetResult(null);
      }

      this.state = StateClosing;
    }

    private Task WaitQueueAsync()
    {
      lock (mutexWaitQueue)
      {
        return completeSource.Task;
      }
    }

    private void ResetWaitEvent()
    {
      lock (mutexWaitQueue)
      {
        completeSource = new TaskCompletionSource<object>();
      }
    }

    private async void Run()
    {
      while (this.state != StateClosed)
      {
        switch (this.state)
        {
          case StateDisconnected:
            {
              try
              {
                if (this.socket == null)
                {
                  this.socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
                }

                await socket.ConnectAsync(host);
                Interlocked.CompareExchange(ref this.state, StateConnected, StateDisconnected);
              }
              catch (Exception e)
              {
                Console.WriteLine("Connect exception: {0}", e.ToString());
                CloseSocket();

                await Task.Delay(1000 * 10);
              }
            }
            break;
          case StateConnected:
            {
              await WaitQueueAsync();

              Message message;
              while (msgQueue.TryDequeue(out message))
              {
                try
                {
                  string jsonString = Message.Serialize(message);
                  byte[] body = Encoding.ASCII.GetBytes(jsonString);
                  byte[] header = BitConverter.GetBytes(body.Length);
                  List<ArraySegment<byte>> dataList = new List<ArraySegment<byte>>()
                  {
                    new ArraySegment<byte>(header),
                    new ArraySegment<byte>(body),
                  };

                  await this.socket.SendAsync(dataList, SocketFlags.None);
                }
                catch (SocketException e)
                {
                  Console.WriteLine("Send exception: {0}", e.ToString());

                  CloseSocket();
                  Interlocked.CompareExchange(ref this.state, StateDisconnected, StateConnected);
                  break;
                }
                catch (Exception e)
                {
                  Console.WriteLine("Send exception: {0}", e.ToString());
                }
              }

              ResetWaitEvent();
            }
            break;
          case StateClosing:
            {
              CloseSocket();
              this.state = StateClosed;
            }
            break;
          case StateClosed:
          default:
            break;
        }
      }
    }

    private void CloseSocket()
    {
      if (this.socket != null)
      {
        this.socket.Close();
        this.socket = null;
      }
    }
  }
}