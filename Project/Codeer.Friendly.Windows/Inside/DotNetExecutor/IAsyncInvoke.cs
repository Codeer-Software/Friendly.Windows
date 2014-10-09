using System;

namespace Codeer.Friendly.DotNetExecutor
{
    /// <summary>
    /// 非同期実行インターフェイス。
    /// </summary>
    public interface IAsyncInvoke
    {
        /// <summary>
        /// 非同期実行。
        /// </summary>
        /// <param name="method">実行メソッド。</param>
        void Execute(AsyncMethod method);
    }

    /// <summary>
    /// 非同期実行メソッド。
    /// </summary>
    public delegate void AsyncMethod();
}
