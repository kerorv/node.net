using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nodes
{
  public class Message
  {
    [JsonIgnore]
    public bool isSystemMsg;
    public Guid from;
    public Guid to;
    public object content;

    public static string Serialize(Message message)
    {
      return JsonConvert.SerializeObject(message);
      // JObject jobject = JObject.FromObject(message);
      // jobject.Add("type", message.content.GetType().ToString());

      // jobject.Add("from", message.from.ToString());
      // jobject.Add("to", message.to.ToString());
      // if (message.content != null)
      // {
      //   jobject["type"] = message.content.GetType().ToString();
      //   JObject jcontent = JObject.FromObject(message.content);
      //   jobject["content"] = JObject.FromObject(message.content);
      // }

      // return jobject.ToString();
    }

    public static Message Deserialize(string jsonString)
    {
      return JsonConvert.DeserializeObject<Message>(jsonString);
/*
      JObject jobject = JObject.Parse(jsonString);

      Message message = new Message();
      message.from = Guid.Parse(jobject["from"].Value<string>());
      message.to = Guid.Parse(jobject["to"].Value<string>());
      if (jobject["type"] != null)
      {
        Type type = Type.GetType(jobject["type"].ToString());
        switch (jobject["content"].Type)
        {
          case JTokenType.Object:
            {
              JObject jcontent = jobject["content"] as JObject;
              message.content = jcontent.ToObject(type);
            }
            break;
          default:
            {
              jobject["content"].Value
            }
            break;
        }
      }

      return message;*/
    }
  }
}