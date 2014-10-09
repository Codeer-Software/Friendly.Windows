using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Codeer.Friendly.Windows.Inside
{
    /// <summary>
    /// 通信を引導
    /// </summary>
    class CommunicationWindow : IDisposable
    {
        const int WM_BEGIN_INVOKE = 0x8200;
        const int WM_INVOKE = 0x8201;
        static readonly IntPtr TimerEventId = new IntPtr(100);

        IntPtr _handle;
        Queue<Delegate> _invokeQueue = new Queue<Delegate>();

        /// <summary>
        /// ハンドル
        /// </summary>
        internal IntPtr Handle 
        {
            get
            {
                if (_handle == IntPtr.Zero)
                {
                    CreateHandle();
                }
                return _handle; 
            } 
        }

        /// <summary>
        /// ハンドル生成
        /// </summary>
        protected void CreateHandle()
        {
            _handle = CommunicationWindowManager.Create(this);
            IntPtr id = NativeMethods.SetTimer(_handle, TimerEventId, 100, IntPtr.Zero);
        }

        /// <summary>
        /// 非同期実行
        /// </summary>
        /// <param name="method">実行メソッド</param>
        internal void BeginInvoke(Delegate method)
        {
            lock (this)
            {
                _invokeQueue.Enqueue(method);

                //キューを独占する可能性があるので、最初の一つのみトリガをかける。
                if (_invokeQueue.Count == 1)
                {
                    NativeMethods.PostMessage(_handle, WM_BEGIN_INVOKE, IntPtr.Zero, IntPtr.Zero);
                }
            }
        }

        /// <summary>
        /// 同期実行
        /// </summary>
        /// <param name="method">実行メソッド</param>
        internal void Invoke(Delegate method)
        {
            lock (this)
            {
                _invokeQueue.Enqueue(method);
            }
            NativeMethods.SendMessage(_handle, WM_INVOKE, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
		/// ファイナライザ
		/// </summary>
        ~CommunicationWindow()
		{
			Dispose(false);
        }

		/// <summary>
		/// 破棄
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
        }

		/// <summary>
		/// 破棄
		/// </summary>
		/// <param name="disposing">破棄フラグ</param>
		protected virtual void Dispose(bool disposing)
		{
            if (disposing && _handle != IntPtr.Zero)
            {
                bool b = NativeMethods.KillTimer(_handle, TimerEventId);
                CommunicationWindowManager.DestroyWindow(_handle);
                _handle = IntPtr.Zero;
            }
        }
       
        /// <summary>
        /// ウィンドウプロック
        /// </summary>
        /// <param name="m">メッセージ</param>
        internal void CallWndProc(ref Message m)
        {
            WndProc(ref m);
        }

        /// <summary>
        /// ウィンドウプロック
        /// </summary>
        /// <param name="m">メッセージ</param>
        protected virtual void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case NativeMethods.WM_TIMER:
                    //PostMessageはキューの数に制限があるため、タイマーで監視を行う。
                    //よほどのことがなければ、ここでキックされることはない。
                    m.Result = new IntPtr(0);
                    lock (this)
                    {
                        if (0 < _invokeQueue.Count)
                        {
                            NativeMethods.PostMessage(_handle, WM_BEGIN_INVOKE, IntPtr.Zero, IntPtr.Zero);
                        }
                    }
                    return;
                case WM_BEGIN_INVOKE:
                    {
                        //一つ実行して、続きがあれば、もう一度投げる。
                        m.Result = new IntPtr(1);
                        Delegate execute = null;
                        lock (this)
                        {
                            if (_invokeQueue.Count == 0)
                            {
                                return;
                            }
                            execute = _invokeQueue.Dequeue();
                            if (_invokeQueue.Count != 0)
                            {
                                NativeMethods.PostMessage(_handle, WM_BEGIN_INVOKE, IntPtr.Zero, IntPtr.Zero);
                            }
                        }
                        execute.DynamicInvoke();
                    }
                    return;
                case WM_INVOKE:
                    {
                        //溜まっているものをすべて実行する。
                        m.Result = new IntPtr(1);
                        while (true)
                        {
                            Delegate execute = null;
                            lock (this)
                            {
                                if (_invokeQueue.Count == 0)
                                {
                                    return;
                                }
                                execute = _invokeQueue.Dequeue();
                            }
                            execute.DynamicInvoke();
                        }
                    }
            }

            //デフォルト
            m.Result = NativeMethods.DefWindowProc(m.HWnd, m.Msg, m.WParam, m.LParam);
        }
    }
}
