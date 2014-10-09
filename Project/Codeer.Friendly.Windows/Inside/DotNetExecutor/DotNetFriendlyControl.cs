#define CODE_ANALYSIS
using System;
using System.ComponentModel;
using Codeer.Friendly.Inside.Protocol;
using System.Diagnostics.CodeAnalysis;

namespace Codeer.Friendly.DotNetExecutor
{
	/// <summary>
	/// .NetのFriendly処理制御。
	/// </summary>
	public class DotNetFriendlyControl
	{
		VarPool _pool = new VarPool();
		TypeFinder _typeFinder = new TypeFinder();

		/// <summary>
		/// 処理呼び出し。
		/// </summary>
		/// <param name="async">非同期実行用。</param>
		/// <param name="info">呼び出し情報。</param>
        /// <returns>戻り値情報。</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public ReturnInfo Execute(IAsyncInvoke async, ProtocolInfo info)
		{
            try
            {
                return DotNetFriendlyExecutor.Execute(async, _pool, _typeFinder, info);
            }
            catch (Exception e)
            {
                return new ReturnInfo(new ExceptionInfo(e));
            }
		}
    }
}
