using System;
using System.Threading.Tasks;
using Nodes;

namespace Nodes.net
{
  class Printer : IService
  {
    public void HandleMessage(object msg)
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
    static async void MakeProcess(Node node)
    {
      Pid pid = await node.CreateProcess(new Printer());
      Console.WriteLine("Create a process.");
    }

    static void Main(string[] args)
    {
      Console.WriteLine("Hello World!");

      Node node = new Node();
      while (true)
      {
        string input = Console.ReadLine();
        if (input == "quit")
        {
          Console.WriteLine("quit.");
          return;
        }
        else
        {
          string[] tokens = input.Split();
          if (tokens.Length == 0)
          {
            continue;
          }

          string command = tokens[0];
          if (command == "c")
          {
            MakeProcess(node);
          }
          else if (command == "p")
          {
            if (tokens.Length < 2)
            {
              Console.WriteLine("wrong command.");
              continue;
            }
          }
          else
          {
            Console.WriteLine("uknown command");
          }
        }
      }
    }
  }
}
