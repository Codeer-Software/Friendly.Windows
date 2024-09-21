using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace Codeer.Friendly.Windows.Inside
{
    class SerializeUtility
    {
        static ICustomSerializer _serializer = new DefaultSerializer();
        static SerializeUtility()
        {
            //さすがに重いか、面倒やけどおくるかな・・・、まあ一旦これで
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(ICustomSerializer).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract && type != typeof(DefaultSerializer))
                    {
                        _serializer = (ICustomSerializer)Activator.CreateInstance(type);
                        break;
                    }
                }
            }
        }

        internal static byte[] Serialize(object obj)=> _serializer.Serialize(obj);
        
        internal static object Deserialize(byte[] bin) => _serializer.Deserialize(bin);

        internal static string GetRequiredAssembliesStartupInfo()
        {
            var list = new List<string>();
            foreach (var assembly in _serializer.GetRequiredAssemblies())
            {
                list.Add(assembly.FullName + "|" + assembly.Location);
            }
            if (list.Count == 0) return string.Empty;

            return string.Join("||", list.ToArray()) + "||";
        }

        class DefaultSerializer : ICustomSerializer
        {

            public byte[] Serialize(object obj)
            {
                var formatter = new BinaryFormatter();
                using (var stream = new MemoryStream())
                {
                    formatter.Serialize(stream, obj);
                    return stream.ToArray();
                }
            }

            public object Deserialize(byte[] bin)
            {
                var formatter = new BinaryFormatter();
                using (var stream = new MemoryStream(bin))
                {
                    return formatter.Deserialize(stream);
                }
            }

            public Assembly[] GetRequiredAssemblies() => new Assembly[0];
        }
    }
}
