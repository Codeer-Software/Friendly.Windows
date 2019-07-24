using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Codeer.Friendly.Windows.Inside
{
    class SerializeUtility
    {
        internal static byte[] Serialize(object obj)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, obj);
                return stream.ToArray();
            }
        }

        internal static object Deserialize(byte[] bin)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream(bin))
            {
                return formatter.Deserialize(stream);
            }
        }
    }
}
