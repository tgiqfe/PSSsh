using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSSsh
{
    internal class Item
    {
        /// <summary>
        /// 実行ファイルへのパス
        /// </summary>
        public static readonly string ExecFilePath =
            System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

        /// <summary>
        /// 実行ファイルの名前
        /// </summary>
        public static readonly string ProcessName =
            System.IO.Path.GetFileNameWithoutExtension(ExecFilePath);

        /// <summary>
        /// 実行ファイルの場所
        /// </summary>
        public static readonly string ExecDirectoryPath =
            System.IO.Path.GetDirectoryName(ExecFilePath);

        /// <summary>
        /// ワークフォルダー
        /// </summary>
        public static readonly string WorkDirectoryPath =
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PSSsh");
    }
}
