using System;
using System.Collections.Generic;
using System.Reflection;

namespace Codeer.Friendly.DotNetExecutor
{
	/// <summary>
	/// 型に関するユーティリティー。
	/// </summary>
	public class TypeFinder
	{
		Dictionary<string, Type> _fullNameAndType = new Dictionary<string, Type>();

		/// <summary>  
		/// タイプフルネームから型を取得する。
		/// </summary>  
		/// <param name="typeFullName">タイプフルネーム。</param>  
		/// <returns>取得した型。</returns>  
		public Type GetType(string typeFullName)
		{
			lock (_fullNameAndType)
			{
				//キャッシュを見る
				Type type = null;
				if (_fullNameAndType.TryGetValue(typeFullName, out type))
				{
					return type;
				}
				
				//各アセンブリに問い合わせる			
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
				List<Type> assemblyTypes = new List<Type>();
				foreach (Assembly assembly in assemblies)
				{
					type = assembly.GetType(typeFullName);
					if (type != null)
					{
						break;
					}
				}
				if (type != null)
				{
					_fullNameAndType.Add(typeFullName, type);
				}
				return type;
			}
		}  
	}
}
