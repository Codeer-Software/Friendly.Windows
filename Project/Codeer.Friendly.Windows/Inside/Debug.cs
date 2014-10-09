#define CODE_ANALYSIS
using System.Windows.Forms;
using System.Diagnostics.CodeAnalysis;

namespace Codeer.Friendly.Windows.Inside
{
    /// <summary>
    /// デバッグ用クラス
    /// </summary>
    static class Debug
    {
        static bool _isDebug;

        /// <summary>
        /// デバッグモードマーク
        /// </summary>
        internal static string DebugMark { get { return _isDebug ? ("???") : (string.Empty); } }

        /// <summary>
        /// トレース
        /// </summary>
        /// <param name="msg">メッセージ</param>
        [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions")]
        internal static void Trace(string msg)
        {
            if (!_isDebug)
            {
                return;
            }
            MessageBox.Show(msg, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.None, MessageBoxDefaultButton.Button1);
        }

        /// <summary>
        /// バッグモードか否かを読み取る
        /// </summary>
        /// <param name="info">情報文字列</param>
        /// <returns>情報コア</returns>
        internal static string ReadDebugMark(string info)
        {
            string infoCore = info.Replace("???", string.Empty);
            _isDebug = (info != infoCore);
            return infoCore;
        }
    }
}
