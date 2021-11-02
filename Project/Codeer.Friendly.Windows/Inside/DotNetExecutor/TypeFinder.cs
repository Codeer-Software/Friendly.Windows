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
		TypeFinderCore _core = new TypeFinderCore();

		/// <summary>
		/// 型を取得
		/// </summary>
		/// <param name="typeFullName">型文字列</param>
		/// <returns>型</returns>
		public Type GetType(string typeFullName)
		{
			var type = _core.GetType(typeFullName);
			if (type != null) return type;
			var stringType = StringType.Parse(typeFullName);
			if (stringType == null) return null;
			return stringType.MakeType(_core);
		}

		/// <summary>
		/// 型に関するユーティリティー。
		/// </summary>
		internal class TypeFinderCore
		{
			Dictionary<string, Type> _fullNameAndType = new Dictionary<string, Type>();
			/// <summary>
			/// タイプフルネームから型を取得する。
			/// </summary>
			/// <param name="typeFullName">タイプフルネーム。</param>
			/// <returns>取得した型。</returns>
			internal Type GetType(string typeFullName)
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

		/// <summary>
		/// 解析後の型情報クラス
		/// </summary>
		public class StringType
		{
			/// <summary>
			/// 型名
			/// </summary>
			public string FullName { get; set; }
			/// <summary>
			/// 型パラメーター情報
			/// </summary>
			public List<StringType> GenericTypes { get; set; } = new List<StringType>();
			internal Type MakeType(TypeFinderCore core)
			{
				var type = core.GetType(FullName);
				if (type == null) return null;
				if (GenericTypes.Count <= 0) return type;
				var genericTypes = new List<Type>();
				foreach (var typeTmp in GenericTypes)
				{
					genericTypes.Add(typeTmp.MakeType(core));
				}

				try
				{
					return type.MakeGenericType(genericTypes.ToArray());
				}
				catch { }

				return null;
			}

			/// <summary>
			/// 型名の付加情報を削除してから変換する為に解析する
			/// </summary>
			/// <param name="typeFullName">型名</param>
			/// <returns>解析後の型情報</returns>
			internal static StringType Parse(string typeFullName)
			{
				var genelicCountIndex = typeFullName.IndexOf('`');
				// 普通のタイプ
				if (genelicCountIndex == -1)
				{
					return new StringType { FullName = typeFullName.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)[0] };
				}
				// ジェネリック
				var genericTypeStart = typeFullName.IndexOf('[');
				var genericType = new StringType { FullName = typeFullName.Substring(0, genericTypeStart) };
				var genericCount = int.Parse(typeFullName.Substring(genelicCountIndex + 1, genericTypeStart - genelicCountIndex - 1));
				var scope = 0;
				// 下記例の「System.Int32」の開始位置
				// System.Collections.Generic.List`1[[System.Int32, System.Private.CoreLib, Version = 5.0.0.0, Culture = neutral, PublicKeyToken = 7cec85d7bea7798e]]
				var typeInfoStartIndex = -1;
				for (int i = genericTypeStart + 1; i < typeFullName.Length; i++)
				{
					if (typeFullName[i] == '[')
					{
						if (typeInfoStartIndex == -1) typeInfoStartIndex = i;
						scope++;
					}
					else if (typeFullName[i] == ']')
					{
						scope--;
					}
					else
					{
						continue;
					}
					if (scope == 0)
					{
						genericType.GenericTypes.Add(Parse(typeFullName.Substring(typeInfoStartIndex + 1, i - typeInfoStartIndex - 1)));
						if (genericType.GenericTypes.Count == genericCount) break;
						typeInfoStartIndex = -1;
					}
				}
				return genericType;
			}
		}
	}
}
