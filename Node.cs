using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nodes
{
  public sealed class Node
  {
    private class ProcessInfo
    {
      internal Process Process { get; set; }
      internal IPEndPoint Address { get; set; }
    }
    private ConcurrentDictionary<Guid, ProcessInfo> pis = new ConcurrentDictionary<Guid, ProcessInfo>();

    private Net.Hub hub;
    private static readonly Lazy<Node> lazy = new Lazy<Node>(() => new Node());
    public static Node Instance { get { return lazy.Value; } }

    private Node()
    {
      hub = new Net.Hub(new Net.DispatchRemoteMessageDelegate((from, message)=>PostMessage(message)));
    }

    public void Start()
    {
      hub.Start();
    }

    public void Close()
    {
      hub.Close();
    }

    public void PostMessage(Message msg)
    {
      ProcessInfo pi;
      if (!pis.TryGetValue(msg.to, out pi))
      {
        return;
      }

      if (pi.Process != null)
      {
        // local process
        pi.Process.PostMessage(msg);
        return;
      }

      // remote process
      if (pi.Address == null)
      {
        // query address
        pi.Address = GetProcessAddress(msg.to);
      }

      hub.Send(pi.Address, msg);
    }

    public IPEndPoint GetProcessAddress(Guid pid)
    {
      throw new NotImplementedException();
    }

    public async Task<Guid> CreateProcess(IService service)
    {
      var task = await Task.Factory.StartNew(async () =>
        {
          Process process = new Process(service);
          await service.Init(process);
          pis.TryAdd(process.Pid, new ProcessInfo { Process = process });
          return process.Pid;
        });

      return task.Result;
    }

    public async Task ExitProcess(Guid pid)
    {
      ProcessInfo pi;
      if (!pis.TryRemove(pid, out pi))
      {
        return;
      }

      if (pi.Process == null)
      {
        // can't exit remote process
        return;
      }

      await pi.Process.Exit(false);
    }

    public async Task TerminateProcess(Guid pid)
    {
      ProcessInfo pi;
      if (!pis.TryRemove(pid, out pi))
      {
        return;
      }

      if (pi.Process == null)
      {
        // can't exit remote process
        return;
      }

      await pi.Process.Exit(true);
    }

    public T GetService<T>(Guid pid) where T : class
    {
      ProcessInfo pi;
      if (!pis.TryGetValue(pid, out pi))
      {
        return null;
      }

      if (pi.Process != null)
      {
        // local process
        return pi.Process.GetService<T>();
      }

      if (pi.Address != null)
      {
        // remote process
        // TODO
      }

      return null;
    }
  }
}