﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace Codeer.Friendly.Windows.Inside
{
    class SerializeUtility
    {
        internal static ICustomSerializer Serializer { get; set; } = new DefaultSerializer();
        static SerializeUtility()
        {
            //さすがに重いか、面倒やけどおくるかな・・・、まあ一旦これで
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (typeof(ICustomSerializer).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract && type != typeof(DefaultSerializer))
                    {
                        Serializer = (ICustomSerializer)Activator.CreateInstance(type);
                        break;
                    }
                }
            }
        }

        internal static byte[] Serialize(object obj)=> Serializer.Serialize(obj);
        
        internal static object Deserialize(byte[] bin) => Serializer.Deserialize(bin);

        internal static string GetRequiredAssembliesStartupInfo()
        {
            var list = new List<string>();
            foreach (var assembly in Serializer.GetRequiredAssemblies())
            {
                list.Add(assembly.FullName + "|" + assembly.Location);
            }
            if (list.Count == 0) return string.Empty;

            return string.Join("||", list.ToArray()) + "||";
        }
    }
}
