using System;
using System.Threading.Tasks;
using Nodes;

namespace Nodes.net
{
  class Printer : IService
  {
    public Task HandleMessage(Message msg)
    {
      throw new NotImplementedException();
    }

    public Task<bool> Init(Process process)
    {
      Console.WriteLine("Printer::Init");
      return Task.FromResult(true);
    }

    public Task Release()
    {
      Console.WriteLine("Printer::Release");
      return Task.CompletedTask;
    }
  }

  class Program
  {
    static async void MakeProcess()
    {
      Guid pid = await Node.Instance.CreateProcess(new Printer());
      Console.WriteLine("Create a process[{0}].", pid);
    }

    static async void ExitProcess(Guid pid)
    {
      await Node.Instance.ExitProcess(pid);
      Console.WriteLine("Process[{0}] exit.", pid);
    }

    static async void TerminateProcess(Guid pid)
    {
      await Node.Instance.TerminateProcess(pid);
      Console.WriteLine("Process[{0}] exit.", pid);
    }

    static void Main(string[] args)
    {
      Console.WriteLine("Hello World!");

      while (true)
      {
        string input = Console.ReadLine();
        if (input == "quit")
        {
          Console.WriteLine("quit.");
          break;
        }
        else
        {
          string[] tokens = input.Split();
          if (tokens.Length == 0)
          {
            continue;
          }

          string command = tokens[0];
          if (command == "CreateProcess")
          {
            MakeProcess();
          }
          else if (command == "ExitProcess")
          {
            if (tokens.Length < 2)
            {
              Console.WriteLine("invalid param.");
              continue;
            }

            Guid pid = Guid.Parse(tokens[1]);
            ExitProcess(pid);
          }
          else if (command == "TerminateProcess")
          {
            if (tokens.Length < 2)
            {
              Console.WriteLine("invalid param.");
              continue;
            }

            Guid pid = Guid.Parse(tokens[1]);
            TerminateProcess(pid);
          }
          else
          {
            Console.WriteLine("unknown command");
          }
        }
      }
    }
  }
}
