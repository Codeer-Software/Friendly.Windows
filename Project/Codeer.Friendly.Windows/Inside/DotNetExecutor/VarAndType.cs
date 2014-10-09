using System;

namespace Codeer.Friendly.DotNetExecutor
{
    /// <summary>
    /// 変数とタイプ。
    /// タイプをマルチスレッドで参照するため、生成時に取得しておく。
    /// </summary>
    class VarAndType
    {
        object _core;
        Type _type;

        /// <summary>
        /// コア
        /// </summary>
        internal object Core { get { return _core; } }

        /// <summary>
        /// タイプ
        /// </summary>
        internal Type Type { get { return _type; } }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="core">コア</param>
        internal VarAndType(object core)
        {
            _core = core;
            if (core != null)
            {
                _type = core.GetType();
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="core">コア</param>
        /// <param name="type">タイプ</param>
        internal VarAndType(object core, Type type)
        {
            _core = core;
            _type = type;
        }
    }
}
