using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nodes.Net
{
  internal class IncomingChannel
  {
    private Socket socket;
    private const int BufferInitSize = 1024;
    private const int PacketHeaderSize = sizeof(int);
    private byte[] buffer = new byte[BufferInitSize];
    private int nextRecvPos;

    internal IncomingChannel(Socket socket)
    {
      this.socket = socket;
    }

    internal async void Start()
    {
      ArraySegment<byte> segment = new ArraySegment<byte>(
        buffer, this.nextRecvPos, buffer.Length - this.nextRecvPos);
      int recvBytes = await this.socket.ReceiveAsync(segment, SocketFlags.None);
      this.nextRecvPos += recvBytes;
      int parsePos = ParsePacket();
      if (parsePos != 0)
      {
        Array.Copy(buffer, parsePos, buffer, 0, this.nextRecvPos - parsePos);
        this.nextRecvPos = 0;
      }
    }

    private int ParsePacket()
    {
      int idx = 0;
      while (idx < this.nextRecvPos)
      {
        if (idx + PacketHeaderSize >= this.nextRecvPos)
        {
          break;
        }

        int packetBodyLength = BitConverter.ToInt32(buffer, idx);
        if (idx + PacketHeaderSize + packetBodyLength >= this.nextRecvPos)
        {
          break;
        }

        try
        {
          string jsonString = Encoding.ASCII.GetString(buffer, idx, packetBodyLength);
          Message message = Message.Deserialize(jsonString);
          OnMessage(message);
        }
        finally
        {
          idx += PacketHeaderSize;
        }
      }

      return idx;
    }

    private void OnMessage(Message message)
    {
    }
  }
}