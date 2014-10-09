using System.Collections.Generic;
using Codeer.Friendly.Inside.Protocol;
using Codeer.Friendly.Inside;
using System;
using Codeer.Friendly.Windows.Inside;

namespace Codeer.Friendly.DotNetExecutor
{
	/// <summary>
	/// .Netの変数管理。
    /// このクラスで定義されているメソッドに関してはスレッドセーフで扱うことができる。
	/// </summary>
	class VarPool
	{
		UniqueNoManager _varAddressManager = new UniqueNoManager();
        Dictionary<int, VarAndType> _handleAndObject = new Dictionary<int, VarAndType>();
        Dictionary<int, int> _keepAlive = new Dictionary<int, int>();

        /// <summary>
        /// 存命登録。
        /// </summary>
        /// <param name="varAddress">変数アドレス。</param>
        internal void KeepAlive(VarAddress varAddress)
        {
            lock (this)
            {
                int count = 0;
                if (!_keepAlive.TryGetValue(varAddress.Core, out count))
                {
                    count = 0;
                }
                count++;
                _keepAlive.Remove(varAddress.Core);
                _keepAlive.Add(varAddress.Core, count);
            }
        }

        /// <summary>
        /// 存命解除。
        /// </summary>
        /// <param name="varAddress">変数アドレス。</param>
        internal void FreeKeepAlive(VarAddress varAddress)
        {
            lock (this)
            {
                int count = 0;
                if (!_keepAlive.TryGetValue(varAddress.Core, out count))
                {
                    return;
                }
                count--;
                if (count <= 0)
                {
                    _keepAlive.Remove(varAddress.Core);
                    if (!_handleAndObject.ContainsKey(varAddress.Core))
                    {
                        //すでに変数プールから削除されているなら番号解放
                        _varAddressManager.FreeNo(varAddress.Core);
                    }
                }
                else
                {
                    _keepAlive[varAddress.Core] = count;
                }
            }
        }

		/// <summary>
		/// 追加。
		/// </summary>
		/// <param name="obj">オブジェクト。</param>
        /// <returns>変数アドレス。</returns>
		internal VarAddress Add(object obj)
		{
			lock (this)
			{
				int no;
				if (!_varAddressManager.CreateNo(out no))
				{
                    throw new InformationException(ResourcesLocal.Instance.OutOfMemory);
				}
				_handleAndObject.Add(no, new VarAndType(obj));
				return new VarAddress(no);
			}
		}

		/// <summary>
		/// 削除。
		/// </summary>
        /// <param name="varAddress">変数アドレス。</param>
		/// <returns>成否。</returns>
        internal bool Remove(VarAddress varAddress)
		{
			lock (this)
			{
                if (!_handleAndObject.ContainsKey(varAddress.Core))
				{
					return false;
				}
                _handleAndObject.Remove(varAddress.Core);
                if (!_keepAlive.ContainsKey(varAddress.Core))
                {
                    //存命中でなければ番号解放
                    _varAddressManager.FreeNo(varAddress.Core);
                }
				return true;
			}
		}

        /// <summary>
        /// 変数取得。
        /// </summary>
        /// <param name="varAddress">変数アドレス。</param>
        /// <returns>変数。</returns>
        internal VarAndType GetVarAndType(VarAddress varAddress)
        {
            lock (this)
            {
                VarAndType varAndType;
                if (_handleAndObject.TryGetValue(varAddress.Core, out varAndType))
                {
                    return new VarAndType(varAndType.Core, varAndType.Type);
                }
                throw new InternalException();
            }
        }

        /// <summary>
		/// オブジェクトの設定。
		/// </summary>
        /// <param name="varAddress">変数アドレス。</param>
		/// <param name="obj">オブジェクト。</param>
        internal void SetObject(VarAddress varAddress, object obj)
		{
			lock (this)
			{
                //非同期実行時には、値を設定に来ても、受け取る変数自体が削除されていることがありうる
                if (_handleAndObject.ContainsKey(varAddress.Core))
                {
                    _handleAndObject[varAddress.Core] = new VarAndType(obj);
                }
			}
		}

        /// <summary>
        /// 指定の変数アドレスが存在しない、またはnullであるか。
        /// このメソッドはスレッドセーフである。
        /// </summary>
        /// <param name="varAddress">変数アドレス。</param>
        /// <returns>指定の変数アドレスが存在しない、またはnullであるか。</returns>
        internal bool IsEmptyVar(VarAddress varAddress)
        {
            lock (this)
            {
                VarAndType varAndType;
                if (_handleAndObject.TryGetValue(varAddress.Core, out varAndType))
                {
                    return object.ReferenceEquals(varAndType.Core, null);
                }
                return true;
            }
        }
    }
}
