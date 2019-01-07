using System.Threading.Tasks;

namespace Nodes
{
  public interface IService
  {
    Task<bool> Init(Process process);
    void HandleMessage(object msg);
    Task Release();
  }
}