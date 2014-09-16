using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;

namespace HookRegistry
{
    public class TagSerializer : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(List<Network.Entity.Tag>).IsAssignableFrom(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var tags = value as List<Network.Entity.Tag>;
            writer.WriteStartObject();
            foreach (var tag in tags)
            {
                var name = Enum.GetName(typeof(GAME_TAG), tag.Name);
                var val = tag.Value;
                writer.WritePropertyName(name);
                writer.WriteValue(val);
            }
            writer.WriteEndObject();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
    public class HookRegistry
    {
        public static TextWriter log;
        static JsonSerializer test;
        public static object OnCall(params object[] args)
        {
            if (args.Length >= 1)
            {
                var rmh = (RuntimeMethodHandle)args[0];
                var method = MethodBase.GetMethodFromHandle(rmh);
                Debug.Log(String.Format("{0}.{1}(...)", method.DeclaringType.FullName, method.Name));
                if (log == null)
                {
                    var fs = File.Open("entity.log", FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                    log = new StreamWriter(fs);
                }
                if (method.DeclaringType.FullName.EndsWith("GameState") && method.Name == "OnPowerHistory")
                {
                    var historyList = args[2] as List<Network.PowerHistory>;
                    test.Serialize(log, historyList);
                    log.WriteLine();
                    log.Flush();
                }
                return null;
            }
            else
            {
                Debug.Log("this is not right");
                return null;
            }
        }

        static HookRegistry()
        {
            test = JsonSerializer.Create();
            test.Converters.Add(new TagSerializer());
        }
    }
}
