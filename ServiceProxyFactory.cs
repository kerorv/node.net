namespace Nodes
{
  public class ServiceProxy<T> where T : class
  {
    public T Service {get;}
    private long instanceId;
    private Pid pid;
    
    public ServiceProxy(long instanceId)
    {
      this.instanceId = instanceId;
    }

  }

  public static class ServiceProxyFactory
  {
/*  public static T CreateServiceProxy<T>(long instanceId) where T : class
    {
    }*/
  }
}