using System.Net;

namespace Nodes
{
  public struct Pid
  {
    public long id;
    public IPEndPoint address;

    public override bool Equals(object obj)
    {
      if (obj is Pid another)
      {
        return (id == another.id);
      }

      return false;
    }
    
    public override int GetHashCode()
    {
        return id.GetHashCode();
    }
  }
}