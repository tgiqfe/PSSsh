using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace PSSsh
{
    internal class Item
    {
        /// <summary>
        /// アプリケーション名
        /// </summary>
        public const string APPLICATION_NAME = "PSSsh";

        /// <summary>
        /// SSH接続時に使用するポート番号。未指定の場合に使用するデフォルトの値
        /// </summary>
        public const int DEFAULT_PORT = 22;

        /// <summary>
        /// SSH接続時のサーバ接続までのタイムアウト値 (秒)
        /// </summary>
        public const int CONNECT_TIMEOUT_SECOND = 10;

        /// <summary>
        /// 作業フォルダー
        /// 変更しない限り、C:\ProgramData\PSSsh で設定
        /// </summary>
        public static string WORK_DIRECTORY = Path.Combine(
            Environment.ExpandEnvironmentVariables("%ProgramData%"), APPLICATION_NAME);

        /// <summary>
        /// ログファイル保存先フォルダー
        /// 変更しない限り、C:\ProgramData\PSSsh\Logs で設定
        /// </summary>
        public static string LOG_DIRECTORY = Path.Combine(WORK_DIRECTORY, "Logs");

        /// <summary>
        /// ログ出力用インスタンス。複数インスタンスからアクセスする。
        /// </summary>
        public static NLog.Logger Logger = null;

        /// <summary>
        /// 一時的に別ディレクトリに移動する為、カレントディレクトリを保存
        /// </summary>
        public static string CurrentDirectory = null;
    }
}
