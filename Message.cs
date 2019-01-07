using System;
using Newtonsoft.Json.Linq;

namespace Nodes
{
  public struct Message
  {
    public bool isSystemMsg;
    public Pid from;
    public Pid to;
    public object content;

    public static string Serialize(Message message)
    {
      JObject jobject = new JObject();
      jobject["from"] = message.from.id;
      jobject["to"] = message.to.id;
      if (message.content != null)
      {
        jobject["type"] = message.content.GetType().ToString();
        jobject["content"] = JObject.FromObject(message.content);
      }

      return jobject.ToString();
    }

    public static Message Deserialize(string jsonString)
    {
      JObject jobject = JObject.Parse(jsonString);

      Message message = new Message();
      message.from.id = jobject["from"].Value<long>();
      message.to.id = jobject["to"].Value<long>();
      if (jobject["type"] != null)
      {
        Type type = Type.GetType(jobject["type"].ToString());
        JObject jcontent = jobject["content"] as JObject;
        message.content = jcontent.ToObject(type);
      }

      return message;
    }
  }
}