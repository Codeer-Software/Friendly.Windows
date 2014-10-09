using System;
using System.Reflection;
using System.Collections;
using System.ComponentModel;
using System.Collections.Generic;
using Codeer.Friendly;
using Codeer.Friendly.Inside.Protocol;
using Codeer.Friendly.Inside;
using System.Globalization;
using System.Text;
using Codeer.Friendly.Windows.Inside;

namespace Codeer.Friendly.DotNetExecutor
{
	/// <summary>
	/// .Netでの処理呼び出し。
	/// </summary>
	static class DotNetFriendlyExecutor
	{
		/// <summary>
		/// 処理呼び出し。
		/// </summary>
		/// <param name="async">非同期実行用。</param>
		/// <param name="varManager">変数管理。</param>
		/// <param name="typeFinder">タイプ検索。</param>
		/// <param name="info">呼び出し情報。</param>
        /// <returns>戻り値情報。</returns>
        internal static ReturnInfo Execute(IAsyncInvoke async, VarPool varManager, TypeFinder typeFinder, ProtocolInfo info)
		{
			switch (info.ProtocolType)
			{
                case ProtocolType.AsyncResultVarInitialize:
				case ProtocolType.VarInitialize:
					return VarInitialize(varManager, info);
				case ProtocolType.VarNew:
					return VarNew(varManager, typeFinder, info);
				case ProtocolType.BinOff:
					return BinOff(varManager, info);
				case ProtocolType.GetValue:
					return GetValue(varManager, info);
				case ProtocolType.SetValue:
					return SetValue(varManager, info);
				case ProtocolType.GetElements:
					return GetElements(varManager, info);
				case ProtocolType.AsyncOperation:
					return AsyncOperation(async, varManager, typeFinder, info);
                case ProtocolType.IsEmptyVar:
                    return IsEmptyVar(varManager, info);
				default:
					throw new InternalException();
			}
		}

        /// <summary>
        /// 空の変数であるか
        /// </summary>
        /// <param name="varManager">変数管理。</param>
        /// <param name="info">呼び出し情報。</param>
        /// <returns>戻り値情報。</returns>
        static ReturnInfo IsEmptyVar(VarPool varManager, ProtocolInfo info)
        {
            return new ReturnInfo(varManager.IsEmptyVar((VarAddress)info.Arguments[0])); 
        }

		/// <summary>
		/// 変数初期化。
		/// </summary>
		/// <param name="varManager">変数管理。</param>
		/// <param name="info">呼び出し情報。</param>
        /// <returns>戻り値情報。</returns>
        static ReturnInfo VarInitialize(VarPool varManager, ProtocolInfo info)
		{
			//初期化は引数は1であること。
			if (info.Arguments.Length != 1)
			{
                throw new InternalException();
			}

			//引数の解決
			object[] args;
			ResolveArgs(varManager, info.Arguments, out args);

			//変数登録
			return new ReturnInfo(varManager.Add(args[0]));
		}

		/// <summary>
		/// 生成処理呼び出し。
		/// </summary>
		/// <param name="varManager">変数管理。</param>
        /// <param name="typeFinder">タイプ検索。</param>
		/// <param name="info">呼び出し情報。</param>
        /// <returns>戻り値情報。</returns>
        static ReturnInfo VarNew(VarPool varManager, TypeFinder typeFinder, ProtocolInfo info)
		{
			//処理可能な型であるか判断
			Type type = typeFinder.GetType(info.TypeFullName);
			if (type == null)
			{
                throw new InformationException(string.Format(CultureInfo.CurrentCulture, ResourcesLocal.Instance.UnknownTypeInfoFormat, info.TypeFullName));
			}

			//引数の解決
			object[] args;
            Type[] argTypesOri;
            ResolveArgs(varManager, info.Arguments, out args, out argTypesOri);
            Type[] argTypes = GetArgTypes(typeFinder, info.OperationTypeInfo, argTypesOri);

            //引数が0でかつ値型の場合
            if (argTypes.Length == 0 && type.IsValueType)
            {
                //変数登録
                return new ReturnInfo(varManager.Add(Activator.CreateInstance(type)));
            }

			//オーバーロードの解決
			ConstructorInfo[] constructorInfos = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
			List<ConstructorInfo> constructorList = new List<ConstructorInfo>();
            bool isObjectArrayArg = false;
			for (int i = 0; i < constructorInfos.Length; i++)
			{
				ParameterInfo[] paramInfos = constructorInfos[i].GetParameters();
                bool isPerfect;
                bool isObjectArrayArgTmp;
                if (IsMatchParameter(info.OperationTypeInfo != null, argTypes, paramInfos, out isPerfect, out isObjectArrayArgTmp))
				{
                    if (isPerfect)
                    {
                        constructorList.Clear();
                        constructorList.Add(constructorInfos[i]);
                        break;
                    }
					constructorList.Add(constructorInfos[i]);
				}
                if (isObjectArrayArgTmp)
                {
                    isObjectArrayArg = true;
                }
			}

            //発見できなかった。
            if (constructorList.Count == 0)
            {
                if (isObjectArrayArg)
                {
                    throw new InformationException(string.Format(CultureInfo.CurrentCulture, ResourcesLocal.Instance.ErrorNotFoundConstractorFormatForObjectArray,
                        type.Name, MakeErrorInvokeArgInfo(argTypes)));
                }
                else
                {
                    throw new InformationException(string.Format(CultureInfo.CurrentCulture, ResourcesLocal.Instance.ErrorNotFoundConstractorFormat,
                        type.Name, MakeErrorInvokeArgInfo(argTypes)));
                }
            }
			if (constructorList.Count != 1)
			{
                //オーバーロード解決に失敗
                throw new InformationException(string.Format(CultureInfo.CurrentCulture, ResourcesLocal.Instance.ErrorManyFoundConstractorFormat,
                        type.Name, MakeErrorInvokeArgInfo(argTypes)));
			}

			//インスタンス生成
			object instance = constructorList[0].Invoke(args);

			//ref, outの解決
			ReflectArgsAfterInvoke(varManager, info.Arguments, args);

			//変数登録
			return new ReturnInfo(varManager.Add(instance));
		}

		/// <summary>
		/// 変数破棄。
		/// </summary>
		/// <param name="varManager">変数管理。</param>
		/// <param name="info">呼び出し情報。</param>
        /// <returns>戻り値情報。</returns>
        static ReturnInfo BinOff(VarPool varManager, ProtocolInfo info)
		{
            varManager.Remove(info.VarAddress);
			return new ReturnInfo();
		}

		/// <summary>
		/// 値取得処理呼び出し。
		/// </summary>
		/// <param name="varManager">変数管理。</param>
		/// <param name="info">呼び出し情報。</param>
        /// <returns>戻り値情報。</returns>
        static ReturnInfo GetValue(VarPool varManager, ProtocolInfo info)
		{
			//引数の数は0であること
			if (info.Arguments.Length != 0)
			{
                throw new InternalException();
			}
            return new ReturnInfo(varManager.GetVarAndType(info.VarAddress).Core);
		}

		/// <summary>
		/// 値設定処理呼び出し。
		/// </summary>
		/// <param name="varManager">変数管理。</param>
		/// <param name="info">呼び出し情報。</param>
        /// <returns>戻り値情報。</returns>
        static ReturnInfo SetValue(VarPool varManager, ProtocolInfo info)
		{
			//引数の数は1であること
			if (info.Arguments.Length != 1)
			{
                throw new InternalException();
			}

			//引数の解決
			object[] args;
			ResolveArgs(varManager, info.Arguments, out args);

			//値の設定
			varManager.SetObject(info.VarAddress, args[0]);
			return new ReturnInfo();
		}

		/// <summary>
		/// 内部要素取得処理呼び出し。
		/// </summary>
		/// <param name="varManager">変数管理。</param>
		/// <param name="info">呼び出し情報。</param>
        /// <returns>戻り値情報。</returns>
        static ReturnInfo GetElements(VarPool varManager, ProtocolInfo info)
		{
            //変数の解決
            object obj = varManager.GetVarAndType(info.VarAddress).Core;

			//この処理が可能なのはIEnumerableを継承している場合
			IEnumerable enumerable = obj as IEnumerable;
			if (enumerable == null)
			{
                throw new InformationException(ResourcesLocal.Instance.HasNotEnumerable);
			}

			//要素をすべて変数登録
			List<VarAddress> list = new List<VarAddress>();
			foreach(object element in enumerable)
			{
				list.Add(varManager.Add(element));
			}
			return new ReturnInfo(list.ToArray());
		}

        /// <summary>
        /// 実行名称と引数によって実施するオペレーションを判断する。
        /// 非同期実行部分以外のコードはスレッドセーフでなければならない。
        /// </summary>
        /// <param name="async">非同期実行用。</param>
        /// <param name="varManager">変数管理。</param>
        /// <param name="typeFinder">タイプ検索。</param>
        /// <param name="info">呼び出し情報。</param>
        /// <returns>戻り値情報。</returns>
        static ReturnInfo AsyncOperation(IAsyncInvoke async, VarPool varManager, TypeFinder typeFinder, ProtocolInfo info)
        {
            Type type;
            object obj;
            object[] args;
            Type[] argTypesOri;
            BindingFlags bind;

            //実行対象の解決
            ResolveInvokeTarget(varManager, typeFinder, info, out type, out obj, out args, out argTypesOri, out bind);

            //第一引数は完了時結果格納変数
            List<object> argTmp = new List<object>(args);
            argTmp.RemoveAt(0);
            args = argTmp.ToArray();
            List<Type> argTypeTmp = new List<Type>(argTypesOri);
            argTypeTmp.RemoveAt(0);
            argTypesOri = argTypeTmp.ToArray();

            //操作解決用型情報取得
            if (info.OperationTypeInfo != null)
            {
                type = typeFinder.GetType(info.OperationTypeInfo.Target);
                if (type == null)
                {
                    throw new InformationException(string.Format(CultureInfo.CurrentCulture, 
                        ResourcesLocal.Instance.UnknownTypeInfoFormat, info.OperationTypeInfo.Target));
                }
            }

            //操作名称最適化
            string operation = OptimizeOperationName(type, info.Operation, args.Length);
            Type findStartType = type;

            //引数の型を取得
            Type[] argTypes = GetArgTypes(typeFinder, info.OperationTypeInfo, argTypesOri);

            //操作検索
            bool isObjectArrayArg = false;
            int nameMatchCount = 0;
            bool first = true;
            bool isAmbiguousArgs = false;
            while (!isAmbiguousArgs && type != null)
            {
                //親の型へ。
                if (!first)
                {
                    type = type.BaseType;
                    if (type == null)
                    {
                        break;
                    }
                }
                first = false;

                //フィールド
                FieldInfo field = FindField(type, bind, operation, args, ref isObjectArrayArg, ref nameMatchCount);
                if (field != null)
                {
                    return ExecuteField(async, varManager, info, obj, args, field);
                }

                //プロパティーとメソッド
                MethodInfo method = FindMethodOrProperty(info.OperationTypeInfo != null, type, bind, operation, argTypes,
                            ref isObjectArrayArg, ref nameMatchCount, ref isAmbiguousArgs);
                if (method != null)
                {
                    return ExecuteMethodOrProperty(async, varManager, info, obj, args, method);
                }
            }

            //結局発見できなかった。
            throw MakeNotFoundException(info, findStartType, argTypes, isObjectArrayArg, nameMatchCount, isAmbiguousArgs);
        }

        /// <summary>
        /// フィールド検索。
        /// </summary>
        /// <param name="type">操作実行対象タイプ。</param>
        /// <param name="bind">操作検索バインディング。</param>
        /// <param name="operation">操作名称。</param>
        /// <param name="args">引数。</param>
        /// <param name="isObjectArrayArg">操作の引数がobject[]型であったか。</param>
        /// <param name="nameMatchCount">名前がマッチした数。</param>
        /// <returns>フィールド情報。</returns>
        static FieldInfo FindField(Type type, BindingFlags bind, string operation, object[] args, 
            ref bool isObjectArrayArg, ref int nameMatchCount)
        {
            FieldInfo field = type.GetField(operation, bind);
            if (field == null)
            {
                return null;
            }
            nameMatchCount++;
            if (1 < args.Length)
            {
                return null;
            }
            //object[]であれば、未発見時のエラーメッセージにそれを表示してやる
            if (field.FieldType == typeof(object[]))
            {
                isObjectArrayArg = true;
            }

            //代入の場合は型チェック
            if (args.Length == 1)
            {
                if (args[0] == null)
                {
                    //null代入不可ならはじく
                    if (!IsAssignableNull(field.FieldType))
                    {
                        return null;
                    }
                }
                //代入可能かチェック
                else if (!field.FieldType.IsAssignableFrom(args[0].GetType()))
                {
                    return null;
                }
            }
            return field;
        }

        /// <summary>
        /// フィールド操作実行。
        /// </summary>
        /// <param name="async">非同期実行用。</param>
        /// <param name="varManager">変数管理。</param>
        /// <param name="info">呼び出し情報。</param>
        /// <param name="obj">実行対象オブジェクト。</param>
        /// <param name="args">操作実行引数。</param>
        /// <param name="field">フィールド情報。</param>
        /// <returns>戻り情報。</returns>
        static ReturnInfo ExecuteField(IAsyncInvoke async, VarPool varManager, ProtocolInfo info,
            object obj, object[] args, FieldInfo field)
        {
            //get
            if (args.Length == 0)
            {
                //戻り値格納用
                VarAddress getVar = varManager.Add(null);

                //番号管理から消されないようにする
                KeepAlive(varManager, info.Arguments, getVar);

                //非同期実行
                async.Execute(delegate
                {
                    ReturnInfo retInfo = new ReturnInfo();
                    try
                    {
                        varManager.SetObject(getVar, field.GetValue(obj));
                    }
                    catch (Exception e)
                    {
                        retInfo = new ReturnInfo(new ExceptionInfo(e));
                    }

                    //完了通知
                    varManager.SetObject((VarAddress)info.Arguments[0], retInfo);

                    //存命状態を解く
                    FreeKeepAlive(varManager, info.Arguments, getVar);
                });
                return new ReturnInfo(getVar);
            }
            //set
            else if (args.Length == 1)
            {
                //番号管理から消されないようにする
                KeepAlive(varManager, info.Arguments, null);

                //非同期実行
                async.Execute(delegate
                {
                    ReturnInfo retInfo = new ReturnInfo();
                    try
                    {
                        field.SetValue(obj, args[0]);
                    }
                    catch (Exception e)
                    {
                        retInfo = new ReturnInfo(new ExceptionInfo(e));
                    }

                    //完了通知
                    varManager.SetObject((VarAddress)info.Arguments[0], retInfo);

                    //存命状態を解く
                    FreeKeepAlive(varManager, info.Arguments, null);
                });
                return new ReturnInfo();
            }
            throw new InternalException();
        }

        /// <summary>
        /// メソッドorプロパティー検索。
        /// </summary>
        /// <param name="isUseOperationTypeInfo">OperationTypeInfoを使っているか。</param>
        /// <param name="type">操作実行対象タイプ。</param>
        /// <param name="bind">操作検索バインディング。</param>
        /// <param name="operation">操作名称。</param>
        /// <param name="argTypes">引数のタイプ。</param>
        /// <param name="isObjectArrayArg">操作の引数がobject[]型であったか。</param>
        /// <param name="nameMatchCount">名前がマッチした数。</param>
        /// <param name="isAmbiguousArgs">あいまいな引数であるか。</param>
        /// <returns>メソッド情報。</returns>
        static MethodInfo FindMethodOrProperty(bool isUseOperationTypeInfo, Type type, BindingFlags bind,
            string operation, Type[] argTypes, ref bool isObjectArrayArg, ref int nameMatchCount, ref bool isAmbiguousArgs)
        {
            //プロパティーとメソッド
            MethodInfo[] methods = type.GetMethods(bind);
            List<MethodInfo> methodList = new List<MethodInfo>();
            for (int i = 0; i < methods.Length; i++)
            {
                //プロパティー指定の場合、メソッドの中からsetter,getterを探して使用する
                if ((methods[i].Name != operation) &&
                    (methods[i].Name != "set_" + operation) &&
                    (methods[i].Name != "get_" + operation))
                {
                    continue;
                }

                nameMatchCount++;

                //引数がマッチするかチェック
                ParameterInfo[] paramInfos = methods[i].GetParameters();
                bool isPerfect;
                bool isObjectArrayArgTmp = false;
                if (IsMatchParameter(isUseOperationTypeInfo, argTypes, paramInfos, out isPerfect, out isObjectArrayArgTmp))
                {
                    if (isPerfect)
                    {
                        methodList.Clear();
                        methodList.Add(methods[i]);
                        break;
                    }
                    methodList.Add(methods[i]);
                }
                if (isObjectArrayArgTmp)
                {
                    isObjectArrayArg = true;
                }
            }

            //マッチする関数が一つだけ見つかった場合は発見したのでループ終了
            if (methodList.Count == 1)
            {
                return methodList[0];
            }
            //複数発見された場合は、オーバーロードの解決ができない。
            else if (1 < methodList.Count)
            {
                isAmbiguousArgs = true;
            }
            return null;
        }

        /// <summary>
        /// メソッドorプロパティー実行。
        /// </summary>
        /// <param name="async">非同期実行用。</param>
        /// <param name="varManager">変数管理。</param>
        /// <param name="info">呼び出し情報。</param>
        /// <param name="obj">実行対象オブジェクト。</param>
        /// <param name="args">操作実行引数。</param>
        /// <param name="method">メソッド情報。</param>
        /// <returns>戻り情報。</returns>
        static ReturnInfo ExecuteMethodOrProperty(IAsyncInvoke async, VarPool varManager,
            ProtocolInfo info, object obj, object[] args, MethodInfo method)
        {
            //戻り値
            VarAddress handle = null;
            if (method.ReturnParameter.ParameterType != typeof(void))
            {
                handle = varManager.Add(null);
            }

            //番号管理から消されないようにする
            KeepAlive(varManager, info.Arguments, handle);

            //非同期実行
            async.Execute(delegate
            {
                ReturnInfo retInfo = new ReturnInfo();
                try
                {
                    object retObj = method.Invoke(obj, args);
                    if (method.ReturnParameter.ParameterType != typeof(void))
                    {
                        varManager.SetObject(handle, retObj);
                    }
                    //ref, outの解決
                    List<object> retArgsTmp = new List<object>();
                    retArgsTmp.Add(null); //完了通知変数を戻す。しかし、ここではまだ格納しない
                    retArgsTmp.AddRange(args);
                    ReflectArgsAfterInvoke(varManager, info.Arguments, retArgsTmp.ToArray());
                }
                catch (Exception e)
                {
                    retInfo = new ReturnInfo(new ExceptionInfo(e));
                }

                //完了通知
                varManager.SetObject((VarAddress)info.Arguments[0], retInfo);

                //存命状態を解く
                FreeKeepAlive(varManager, info.Arguments, handle);
            });

            return new ReturnInfo(handle);
        }

        /// <summary>
        /// 操作を見つけることが出来なかった場合の例外作成。
        /// </summary>
        /// <param name="info">操作情報。</param>
        /// <param name="findStartType">検索開始の型。</param>
        /// <param name="argTypes">型情報。</param>
        /// <param name="isObjectArrayArg">操作の引数がobject[]型であったか。</param>
        /// <param name="nameMatchCount">名前がマッチした数。</param>
        /// <param name="isAmbiguousArgs">あいまいな引数であるか。</param>
        /// <returns>例外。</returns>
        static InformationException MakeNotFoundException(ProtocolInfo info, Type findStartType, Type[] argTypes,
                            bool isObjectArrayArg, int nameMatchCount, bool isAmbiguousArgs)
        {
            if (isAmbiguousArgs)
            {
                throw new InformationException(string.Format(CultureInfo.CurrentCulture, ResourcesLocal.Instance.ErrorManyFoundInvokeFormat,
                    findStartType.Name, info.Operation, MakeErrorInvokeArgInfo(argTypes)));
            }
            else if (nameMatchCount == 0)
            {
                return new InformationException(string.Format(CultureInfo.CurrentCulture, ResourcesLocal.Instance.ErrorNotFoundInvokeFormat,
                    findStartType.Name, info.Operation, MakeErrorInvokeArgInfo(argTypes)));
            }
            else
            {
                if (isObjectArrayArg)
                {
                    return new InformationException(string.Format(CultureInfo.CurrentCulture, ResourcesLocal.Instance.ErrorArgumentInvokeFormatForObjectArray,
                        findStartType.Name, info.Operation, MakeErrorInvokeArgInfo(argTypes)));
                }
                else
                {
                    return new InformationException(string.Format(CultureInfo.CurrentCulture, ResourcesLocal.Instance.ErrorArgumentInvokeFormat,
                        findStartType.Name, info.Operation, MakeErrorInvokeArgInfo(argTypes)));
                }
            }
        }

        /// <summary>
        /// 存命登録。
        /// </summary>
        /// <param name="varManager">変数管理。</param>
        /// <param name="arguments">引数情報。</param>
        /// <param name="handle">戻り値ハンドル。</param>
        static void KeepAlive(VarPool varManager, object[] arguments, VarAddress handle)
        {
            if (handle != null)
            {
                varManager.KeepAlive(handle);
            }
            for (int i = 0; i < arguments.Length; i++)
            {
                VarAddress aliveHandle = arguments[i] as VarAddress;
                if (aliveHandle != null)
                {
                    varManager.KeepAlive(aliveHandle);
                }
            }
        }

        /// <summary>
        /// 存命登録解除。
        /// </summary>
        /// <param name="varManager">変数管理。</param>
        /// <param name="arguments">引数情報。</param>
        /// <param name="handle">戻り値ハンドル。</param>
        static void FreeKeepAlive(VarPool varManager, object[] arguments, VarAddress handle)
        {
            if (handle != null)
            {
                varManager.FreeKeepAlive(handle);
            }
            for (int i = 0; i < arguments.Length; i++)
            {
                VarAddress aliveHandle = arguments[i] as VarAddress;
                if (aliveHandle != null)
                {
                    varManager.FreeKeepAlive(aliveHandle);
                }
            }
        }

        /// <summary>
        /// 操作名称最適化。
        /// </summary>
        /// <param name="type">対象のタイプ。</param>
        /// <param name="operation">操作名称。</param>
        /// <param name="argsLength">引数の数。</param>
        /// <returns>最適化された操作名称。</returns>
        static string OptimizeOperationName(Type type, string operation, int argsLength)
        {
            if (operation.IndexOf("[") != -1)
            {
                string[] a = operation.Replace("[", string.Empty).Replace("]", string.Empty).Split(new char[] { ',' }, StringSplitOptions.None);
                if (a.Length == argsLength)
                {
                    operation = type.IsArray ? "Get" : "get_Item";
                }
                else
                {
                    operation = type.IsArray ? "Set" : "set_Item";
                }
            }
            return operation;
        }

		/// <summary>
		/// 呼び出し対象の解決。
		/// </summary>
		/// <param name="varManager">変数管理。</param>
        /// <param name="typeFinder">タイプ検索。</param>
		/// <param name="info">呼び出し情報。</param>
		/// <param name="type">タイプ。</param>
		/// <param name="targetObj">オブジェクト。</param>
        /// <param name="args">引数。</param>
        /// <param name="argTypes">引数タイプ。</param>
        /// <param name="bind">バインディング。</param>
        static void ResolveInvokeTarget(VarPool varManager, TypeFinder typeFinder, ProtocolInfo info, out Type type, out object targetObj, out object[] args, out Type[] argTypes, out BindingFlags bind)
		{
			type = null;
			targetObj = null;
			bind = BindingFlags.Public | BindingFlags.NonPublic;

			//static呼び出し時
			if (info.VarAddress == null)
			{
				type = typeFinder.GetType(info.TypeFullName);
				if (type == null)
				{
                    throw new InformationException(string.Format(CultureInfo.CurrentCulture, ResourcesLocal.Instance.UnknownTypeInfoFormat, info.TypeFullName));
				}
				bind |= BindingFlags.Static;
			}
			//オブジェクトに対する呼び出し
			else
			{
                VarAndType varAndType = varManager.GetVarAndType(info.VarAddress);
                targetObj = varAndType.Core;
                if (targetObj == null)
                {
                    throw new InformationException(ResourcesLocal.Instance.NullObjectOperation);
                }
                type = varAndType.Type;
				bind |= BindingFlags.Instance;
			}

			//引数の解決
            ResolveArgs(varManager, info.Arguments, out args, out argTypes);
		}

        /// <summary>
        /// 引数の解決。
        /// </summary>
        /// <param name="varManager">変数管理。</param>
        /// <param name="argsInfo">引数情報。</param>
        /// <param name="args">引数。</param>
        internal static void ResolveArgs(VarPool varManager, object[] argsInfo, out object[] args)
        {
            Type[] argTypes;
            ResolveArgs(varManager, argsInfo, out args, out argTypes);
        }
        
        /// <summary>
		/// 引数の解決。
		/// </summary>
		/// <param name="varManager">変数管理。</param>
		/// <param name="argsInfo">引数情報。</param>
        /// <param name="args">引数。</param>
        /// <param name="argTypes">引数の型。</param>
        internal static void ResolveArgs(VarPool varManager, object[] argsInfo, out object[] args, out Type[] argTypes)
		{
			args = new object[argsInfo.Length];
            argTypes = new Type[argsInfo.Length];
			for (int i = 0; i < argsInfo.Length; i++)
			{
				VarAddress handle = argsInfo[i] as VarAddress;

				//値の場合
				if (handle == null)
				{
					args[i] = argsInfo[i];
                    if (args[i] != null)
                    {
                        argTypes[i] = args[i].GetType();
                    }
				}
				//変数の場合は登録されているオブジェクトに変換
				else
				{
					VarAndType varAndType = varManager.GetVarAndType(handle);
                    args[i] = varAndType.Core;
                    argTypes[i] = varAndType.Type;
				}
			}
		}

		/// <summary>
		/// 呼び出し後の引数反映。
		/// </summary>
		/// <param name="varManager">変数管理。</param>
		/// <param name="argsInfo">引数情報。</param>
		/// <param name="args">引数。</param>
		internal static void ReflectArgsAfterInvoke(VarPool varManager, object[] argsInfo, object[] args)
		{
			if (argsInfo.Length != args.Length)
			{
                throw new InternalException();
			}
			for (int i = 0; i < argsInfo.Length; i++)
			{
				VarAddress handle = argsInfo[i] as VarAddress;
				if (handle != null)
				{
					varManager.SetObject(handle, args[i]);
				}
			}
		}

        /// <summary>
        /// 引数型情報取得。
        /// </summary>
        /// <param name="typeFinder">タイプ検索。</param>
        /// <param name="operationTypeInfo">操作型情報。</param>
        /// <param name="argTypesOri">元引数。</param>
        /// <returns>型情報</returns>
        static Type[] GetArgTypes(TypeFinder typeFinder, OperationTypeInfo operationTypeInfo, Type[] argTypesOri)
        {
            List<Type> argTypes = new List<Type>();
            if (operationTypeInfo == null)
            {
                argTypes.AddRange(argTypesOri);
            }
            else
            {
                for (int i = 0; i < operationTypeInfo.Arguments.Length; i++)
                {
                    Type type = typeFinder.GetType(operationTypeInfo.Arguments[i]);
                    if (type == null)
                    {
                        throw new InformationException(string.Format(CultureInfo.CurrentCulture, ResourcesLocal.Instance.UnknownTypeInfoFormat, operationTypeInfo.Arguments[i]));
                    }
                    argTypes.Add(type);
                }
                
                //object[]指定された場合の特殊処理
                if (operationTypeInfo.Arguments.Length == 1 &&
                    operationTypeInfo.Arguments[0] == typeof(object[]).ToString() &&
                    argTypesOri.Length != 1)
                {
                    throw new InformationException(string.Format(CultureInfo.CurrentCulture, ResourcesLocal.Instance.ErrorOperationTypeArgInfoForObjectArrayFormat,
                        MakeErrorInvokeArgInfo(argTypes.ToArray()), MakeErrorInvokeArgInfo(argTypesOri)));
                }

                if (argTypesOri.Length != operationTypeInfo.Arguments.Length)
                {
                    throw new InformationException(string.Format(CultureInfo.CurrentCulture, ResourcesLocal.Instance.ErrorOperationTypeArgInfoFormat,
                        MakeErrorInvokeArgInfo(argTypes.ToArray()), MakeErrorInvokeArgInfo(argTypesOri)));
                }
            }
            return argTypes.ToArray();
        }

        /// <summary>
        /// 引数型からエラー情報を作成する。
        /// </summary>
        /// <param name="argTypes">引数型情報。</param>
        /// <returns>エラー情報。</returns>
        static string MakeErrorInvokeArgInfo(Type[] argTypes)
        {
            StringBuilder builder = new StringBuilder();
            foreach (Type type in argTypes)
            {
                if (0 < builder.Length)
                {
                    builder.Append(", ");
                }
                builder.Append((type == null) ? "null" : type.Name);
            }
            return builder.ToString();
        }

		/// <summary>
		/// パラメータが一致しているか。
        /// </summary>
        /// <param name="isUseOperationTypeInfo">OperationTypeInfoを使っているか。</param>
        /// <param name="args">引数情報。</param>
		/// <param name="paramInfos">パラメータ情報。</param>
        /// <param name="isPerfect">完全一致であるか。</param>
        /// <param name="isObjectArrayArg">オブジェクト配列の引数であったか。</param>
        /// <returns>パラメータが一致しているか。</returns>
        static bool IsMatchParameter(bool isUseOperationTypeInfo, Type[] args, ParameterInfo[] paramInfos, out bool isPerfect, out bool isObjectArrayArg)
		{
            isObjectArrayArg = false;
            if (paramInfos.Length == 1)
            {
                //ret, outの場合は修飾を外す
                Type paramType = (paramInfos[0].ParameterType.IsByRef) ?
                    paramInfos[0].ParameterType.GetElementType() : paramInfos[0].ParameterType;
                if (paramType == typeof(object[]))
                {
                    isObjectArrayArg = true;
                }
            }

			if (args.Length != paramInfos.Length)
			{
                isPerfect = false;
				return false;
			}
            isPerfect = true;
            for (int j = 0; j < paramInfos.Length; j++)
			{
				if (args[j] == null)
				{
                    //null代入不可ならはじく
                    if (!IsAssignableNull(paramInfos[j].ParameterType))
                    {
                        return false;
                    }
                    isPerfect = false;
					continue;
                }				
                
                //OperationTypeInfoを使っていなくて、ret, outの場合は修飾を外す
                Type paramType = (!isUseOperationTypeInfo && paramInfos[j].ParameterType.IsByRef) ?
                    paramInfos[j].ParameterType.GetElementType() : paramInfos[j].ParameterType;

                //完全一致
                if (args[j] == paramType)
                {
                    continue;
                }

                //OperationTypeInfoを使っている場合はByRefが一致しなければならない
                if (isUseOperationTypeInfo && paramType.IsByRef != args[j].IsByRef)
                {
                    return false;
                }

                isPerfect = false;

                //代入チェック時はref,outの修飾を外す
                if (paramType.IsByRef)
                {
                    paramType = paramType.GetElementType();
                }

                //代入可能かの判定
				if (!paramType.IsAssignableFrom(args[j]))
				{
                    return false;
				}
			}
			return true;
		}

        /// <summary>
        /// NULL代入可能であるか。
        /// </summary>
        /// <param name="type">タイプ。</param>
        /// <returns>NULL代入可能であるか。</returns>
        private static bool IsAssignableNull(Type type)
        {
             if (!type.IsValueType)
             {
                 return true;
             }
             return (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }
	}
}
