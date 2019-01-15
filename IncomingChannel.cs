using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Nodes.Net
{
  internal class IncomingChannel
  {
    private Socket socket;
    private volatile bool closed = false;
    private const int BufferInitSize = 1024;
    private const int PacketHeaderSize = sizeof(int);
    private byte[] buffer = new byte[BufferInitSize];
    private int nextRecvPos;
    private DispatchRemoteMessageDelegate dispatcher;

    internal IncomingChannel(Socket socket, DispatchRemoteMessageDelegate dispatcher)
    {
      this.socket = socket;
      this.dispatcher = dispatcher;
    }

    internal async void Start()
    {
      try
      {
        while (true)
        {
          ArraySegment<byte> segment = new ArraySegment<byte>(
            buffer, this.nextRecvPos, buffer.Length - this.nextRecvPos);
          int recvBytes = await this.socket.ReceiveAsync(segment, SocketFlags.None);
          Console.WriteLine("receive {0} bytes.", recvBytes);
          if (recvBytes <= 0)
          {
            CloseSocket();
            return;
          }

          this.nextRecvPos += recvBytes;
          int parsePos = ParsePackets();
          if (parsePos != 0)
          {
            Array.Copy(buffer, parsePos, buffer, 0, this.nextRecvPos - parsePos);
            this.nextRecvPos = 0;
          }
        }
      }
      catch (Exception e)
      {
        Console.WriteLine("IncomingChannel::Start exception: {0}", e.Message);
        CloseSocket();
      }
    }

    internal void Close()
    {
      CloseSocket();
    }

    private int ParsePackets()
    {
      int idx = 0;
      while (idx < this.nextRecvPos)
      {
        if (idx + PacketHeaderSize > this.nextRecvPos)
        {
          break;
        }

        int packetBodyLength = BitConverter.ToInt32(buffer, idx);
        if (packetBodyLength <= 0)
        {
          break;
        }
        if (idx + PacketHeaderSize + packetBodyLength > this.nextRecvPos)
        {
          break;
        }

        try
        {
          string jsonString = Encoding.ASCII.GetString(buffer, idx + PacketHeaderSize, packetBodyLength);
          Message message = Message.Deserialize(jsonString);
          OnMessage(this.socket.RemoteEndPoint as IPEndPoint, message);
        }
        catch (Exception e)
        {
          Console.WriteLine("ParsePackets exception: {0}", e);
        }
        finally
        {
          idx += (PacketHeaderSize + packetBodyLength);
        }
      }

      return idx;
    }

    private void OnMessage(IPEndPoint from, Message message)
    {
      if (dispatcher != null)
      {
        dispatcher(from, message);
      }
    }

    private void CloseSocket()
    {
      if (!this.closed)
      {
        this.socket.Close();
        this.closed = true;
      }
    }
  }
}