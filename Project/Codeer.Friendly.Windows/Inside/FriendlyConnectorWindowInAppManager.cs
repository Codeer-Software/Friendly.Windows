using System;
using System.Collections.Generic;

namespace Codeer.Friendly.Windows.Inside
{
    /// <summary>
    /// Friendly接続ウィンドウ管理。
    /// スレッドセーフである。
    /// </summary>
    class FriendlyConnectorWindowInAppManager
    {
        Dictionary<IntPtr, FriendlyConnectorWindowInApp> _handleAndWindow = new Dictionary<IntPtr, FriendlyConnectorWindowInApp>();

        /// <summary>
        /// 追加。
        /// ウィンドウの所属するスレッドと実行スレッドが違う可能性があるので、ウィンドウからハンドルを取得しない。
        /// </summary>
        /// <param name="handle">ハンドル。</param>
        /// <param name="window">ウィンドウ。</param>
        internal void Add(IntPtr handle, FriendlyConnectorWindowInApp window)
        {
            lock (this)
            {
                _handleAndWindow.Add(handle, window);
            }
        }

        /// <summary>
        /// クローンの作成。
        /// </summary>
        /// <returns>クローン。</returns>
        internal Dictionary<IntPtr, FriendlyConnectorWindowInApp> Clone()
        {
            lock (this)
            {
                return new Dictionary<IntPtr, FriendlyConnectorWindowInApp>(_handleAndWindow);
            }
        }

        /// <summary>
        /// ハンドルから検索。
        /// </summary>
        /// <param name="handle">ハンドル</param>
        /// <returns>Friendly接続ウィンドウ</returns>
        internal FriendlyConnectorWindowInApp FromHandle(IntPtr handle)
        {
            lock (this)
            {
                FriendlyConnectorWindowInApp window;
                return _handleAndWindow.TryGetValue(handle, out window) ? window : null;
            }
        }

        /// <summary>
        /// 削除
        /// </summary>
        /// <param name="handle">ハンドル。</param>
        internal void Remove(IntPtr handle)
        {
            lock (this)
            {
                _handleAndWindow.Remove(handle);
            }
        }
    }
}
