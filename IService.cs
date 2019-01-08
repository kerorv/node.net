using System.Threading.Tasks;

namespace Nodes
{
  public interface IService
  {
    Task<bool> Init(Process process);
    Task HandleMessage(Message msg);
    Task Release();
  }
}