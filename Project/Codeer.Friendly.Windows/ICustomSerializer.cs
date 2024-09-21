using System.Reflection;

namespace Codeer.Friendly.Windows
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICustomSerializer
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        byte[] Serialize(object obj);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="bin"></param>
        /// <returns></returns>
        object Deserialize(byte[] bin);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        Assembly[] GetRequiredAssemblies();
    }
}
