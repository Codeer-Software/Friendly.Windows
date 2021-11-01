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
				type = GetTypeCore(assemblies, typeFullName);
				if (type == null)
				{
					// 見つからない場合は型情報のみ残して再検索する
					// （.NET5から.NET Core3.1への参照等、バージョン違いで変換できる場合は変換し直して使用できるようにする）
					// System.Collections.Generic.List`1[[System.Int32, System.Private.CoreLib, Version = 5.0.0.0, Culture = neutral, PublicKeyToken = 7cec85d7bea7798e]]
					// という名前を
					// System.Collections.Generic.List`1[[System.Int32]]
					// という感じで「,」と「]」の間を削除する
					// ただし、System以外のアセンブリ指定の場合は変換できないのでそのまま使用する
					bool isDelete = false;
					bool isSkip = false;
					string typeFullNameTmp = string.Empty;
					int length = typeFullName.Length;
					for (int i = 0; i < length; i++)
					{
						var tmp = typeFullName.Substring(i, 1);
						switch (tmp)
						{
							case ",":
                                {
									var nextChar = (i < length - 1) ? typeFullName.Substring(i + 1, 1) : string.Empty;
									if (isDelete || isSkip || nextChar == "[")
									{
										break;
									}
									// []の間の文字列を取得して型変換できるかチェックする
									int indexStart = typeFullName.Substring(0, i).LastIndexOf('[') + 1;
									int indexEnd = typeFullName.IndexOf(']', indexStart);
									var typeName = typeFullName.Substring(indexStart, indexEnd - indexStart);
									if (GetTypeCore(assemblies, typeName) != null)
									{
										// 取得できる場合はそのまま使用する
										isSkip = true;
										break;
									}
									isDelete = true;
								}
								continue;
							case "]":
								isSkip = false;
								isDelete = false;
								break;
						}
						if (isDelete)
						{
							continue;
						}
						typeFullNameTmp += tmp;
					}
					type = GetTypeCore(assemblies, typeFullNameTmp);
				}
				if (type != null)
				{
					_fullNameAndType.Add(typeFullName, type);
				}
				return type;
			}
		}  

		/// <summary>
		/// 各アセンブリからフルネーム指定で型を取得
		/// </summary>
		/// <param name="assemblies">検索対象アセンブリ一覧</param>
		/// <param name="typeFullName">タイプフルネーム</param>
		/// <returns>型</returns>
		Type GetTypeCore(Assembly[] assemblies, string typeFullName)
        {
			foreach (Assembly assembly in assemblies)
			{
				var type = assembly.GetType(typeFullName);
				if (type != null)
				{
					return type;
				}
			}

			return null;
		}
	}
}
