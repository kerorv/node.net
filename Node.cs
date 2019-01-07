using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Nodes
{
  class Node
  {
    private ConcurrentDictionary<Pid, Process> processes;
    struct Capacity
    {
      private int memoryPercent;
      private int cpuPercent;
      private int memoryLimit;
    }
    Capacity capacity;
    SortedSet<string> typeRegistry;

    void Start()
    {
      ThreadPool.SetMaxThreads(Environment.ProcessorCount, Environment.ProcessorCount);
    }

    public async Task<ServiceRef<T>> GetSerice<T>(long serviceId) where T : class
    {

    }

    public void PostMessage(Pid target, Message msg)
    {
      Process process = null;
      if (!processes.TryGetValue(target, out process))
      {
        return;
      }

      process.PostMessage(msg);
    }

    public Pid GetServicePid(long instanceId)
    {
      // get name service from configuration
      // TODO:
      Pid nameService;
      nameService.id = 0x0000000100000000;
      nameService.address = "127.0.0.1:10039";

      Message queryMsg;
      queryMsg.isSystemMsg = false;
      queryMsg.from = this.id;
      queryMsg.to = nameService;
      class QueryCommand
      {
        public long InstanceId; 
      }
      queryMsg.content = new QueryCommand(InstanceId = instanceId);
      Nodes.Node.PostMessage(nameService, queryMsg);
    }

    public async Task<Pid> CreateProcess(IService service)
    {
      var task = await Task.Factory.StartNew(async () =>
        {
          Process process = new Process(service);
          await service.Init(process);
          return process.Pid;
        });
      
      return task.Result;
    }

    public async Task ExitProcess(Pid pid)
    {
      Process process = null;
      if (!processes.TryRemove(pid, out process))
      {
        await Task.CompletedTask;
      }

      await process.Exit(false);
    }

    public void TerminateProcess(Pid pid)
    {
      Process process = null;
      if (!processes.TryRemove(pid, out process))
      {
        return;
      }

      process.Exit(true);
    }

    public Task RegisterAppType(Type type)
    {
      return Task.CompletedTask;
      // typeRegistry.Add(type.ToString());
    }
  }
}