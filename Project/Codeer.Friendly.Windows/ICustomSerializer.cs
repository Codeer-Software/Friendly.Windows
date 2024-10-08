using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace Codeer.Friendly.Windows
{
    /// <summary>
    /// ICustomSerializer
    /// </summary>
    public interface ICustomSerializer
    {
        /// <summary>
        /// Serialize
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>binary</returns>
        byte[] Serialize(object obj);

        /// <summary>
        /// Deserialize
        /// </summary>
        /// <param name="bin">binary</param>
        /// <returns>object</returns>
        object Deserialize(byte[] bin);

        /// <summary>
        /// Assembly required for use.
        /// </summary>
        /// <returns>Assemblies</returns>
        Assembly[] GetRequiredAssemblies();
    }

    /// <summary>
    /// DefaultSerializer
    /// </summary>
    public class DefaultSerializer : ICustomSerializer
    {
        /// <summary>
        /// Serialize
        /// </summary>
        /// <param name="obj">object</param>
        /// <returns>binary</returns>
        public byte[] Serialize(object obj)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, obj);
                return stream.ToArray();
            }
        }

        /// <summary>
        /// Deserialize
        /// </summary>
        /// <param name="bin">binary</param>
        /// <returns>object</returns>
        public object Deserialize(byte[] bin)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream(bin))
            {
                return formatter.Deserialize(stream);
            }
        }

        /// <summary>
        /// Assembly required for use.
        /// </summary>
        /// <returns>Assemblies</returns>
        public Assembly[] GetRequiredAssemblies() => new Assembly[0];
    }
}
