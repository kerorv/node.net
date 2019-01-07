using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Nodes
{
  public class Process
  {
    private Pid id;
    private ConcurrentQueue<Message> msgs;
    private enum State { Idle = 0, Running = 1 }
    private State state = State.Idle;
    private IService service;
    private class ExitCommand
    {
      internal TaskCompletionSource<object> exitEvent;
    }

    public Process(IService service)
    {
      this.service = service;
    }

    public Pid Pid { get; }

    public void PostMessage(Message msg)
    {
      msgs.Enqueue(msg);

      lock (this)
      {
        if (state == State.Running)
        {
          return;
        }

        state = State.Running;
      }

      ThreadPool.QueueUserWorkItem(this.Run, null);
    }

    private void Run(object param)
    {
      // check state == Running
      // TODO:

      const int MessageCountOnce = 32;
      for (int i = 0; i < MessageCountOnce; ++i)
      {
        Message msg;
        if (!msgs.TryDequeue(out msg))
        {
          break;
        }

        if (msg.isSystemMsg)
        {
          // handle system message
          if (msg.content is ExitCommand command)
          {
            command.exitEvent.SetResult(null);
            return;
          }
        }
        else
        {
          // handle application message
          this.service?.HandleMessage(msg);
        }
      }

      lock (this)
      {
        if (msgs.IsEmpty)
        {
          state = State.Idle;
          return;
        }
      }

      ThreadPool.QueueUserWorkItem(this.Run, null);
    }

    internal Task Exit(bool interrupt)
    {
      if (interrupt)
      {
        msgs.Clear();
      }

      lock (this)
      {
        if (state == State.Idle)
        {
          if (msgs.IsEmpty || interrupt)
          {
            return Task.CompletedTask;
          }
        }
      }

      var cmd = new ExitCommand();
      cmd.exitEvent = new TaskCompletionSource<object>();

      Message exitMsg = default(Message);
      exitMsg.isSystemMsg = true;
      exitMsg.content = cmd;
      this.PostMessage(exitMsg);
      return cmd.exitEvent.Task;
    }
  }
}