﻿using System;
using System.Reflection;

namespace Codeer.Friendly.Windows.Step
{
    /// <summary>
    /// 開始踏み台
    /// </summary>
    public static class StartStep
    {
        /// <summary>
        /// 開始。
        /// </summary>
        /// <param name="startInfo">開始情報。</param>
        public static int Start(string startInfo)
        {
            AssemblyResolver.EntryAssembly(typeof(StartStep).Assembly);

            //ここでの引数エラーはAssert。
            //でも、通知は仕様的にできない。
            string[] infos = startInfo.Split(new string[] { "||" }, StringSplitOptions.RemoveEmptyEntries);

            //アセンブリ読み込み
            for (int i = 0; i < infos.Length - 3; i++)
            {
                string[] asmInfo = infos[i].Split('|');
                AssemblyResolver.LoadAssembly(asmInfo[0], asmInfo[1]);
            }

            //メソッド実行情報取得
            string szTypeFullName = infos[infos.Length - 3];
            string szMethod = infos[infos.Length - 2];
            string arg = infos[infos.Length - 1];

            //実行
            MethodInfo method = new TypeFinder().GetType(szTypeFullName).GetMethod(szMethod);
            method.Invoke(null, new object[] { arg });
            return 0;
        }
    }
}
