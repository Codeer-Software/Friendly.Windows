using System.Collections.Generic;

namespace Codeer.Friendly.Windows.Inside
{
    class SerializeUtility
    {
        internal static ICustomSerializer Serializer { get; set; } = new DefaultSerializer();

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
