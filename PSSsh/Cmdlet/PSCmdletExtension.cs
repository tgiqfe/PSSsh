using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using Renci.SshNet;
using Renci.SshNet.Common;
using PSSsh.Lib;
using System.Text.RegularExpressions;

namespace PSSsh.Cmdlet
{
    public class PSCmdletExtension : PSCmdlet
    {
        /// <summary>
        /// SSH接続時のサーバ接続までのタイムアウト値(秒)
        /// </summary>
        protected const int CONNECT_TIMEOUT = 10;

        #region Change CurrentDirectory

        private string _currentDirectory = null;

        protected override void BeginProcessing()
        {
            //  カレントディレクトリカレントディレクトリの一時変更
            _currentDirectory = Environment.CurrentDirectory;
            Environment.CurrentDirectory = SessionState.Path.CurrentFileSystemLocation.Path;
        }

        protected override void EndProcessing()
        {
            //  カレントディレクトリを戻す
            Environment.CurrentDirectory = _currentDirectory;
        }

        #endregion
        #region User,Password

        /// <summary>
        /// ユーザー名を決定して返す
        /// </summary>
        /// <param name="user"></param>
        /// <param name="credential"></param>
        /// <returns></returns>
        protected string GetUserName(string user, PSCredential credential, SshSession session)
        {
            if (credential != null)
            {
                return credential.UserName;
            }
            if (string.IsNullOrEmpty(user) && (session?.IsUserEmpty() ?? true))
            {
                Console.Write("User: ");
                user = Console.ReadLine();
            }
            return user;
        }

        /// <summary>
        /// パスワードを決定して返す。
        /// </summary>
        /// <param name="password"></param>
        /// <param name="credential"></param>
        /// <param name="passwordFile"></param>
        /// <returns></returns>
        protected string GetPassword(string password, PSCredential credential, string passwordFile, SshSession session)
        {
            if (credential != null)
            {
                //  Credentialからパスワード読み取り
                return System.Runtime.InteropServices.Marshal.PtrToStringUni(
                    System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(credential.Password));
            }
            else if (!string.IsNullOrEmpty(passwordFile) && File.Exists(passwordFile))
            {
                //  PasswordFileからパスワード読み取り
                try
                {
                    //  ===========================================
                    //  $cred = Get-Credential
                    //  $cred.Password | ConvertFrom-SecureString | Set-Content .\pwoutput.txt
                    //  ===========================================
                    //  等で、PowerShellで暗号化したパスワードファイルをした場合
                    var res = InvokeCommand.InvokeScript(
                        SessionState,
                        InvokeCommand.NewScriptBlock(
                            "[System.Runtime.InteropServices.Marshal]::PtrToStringBSTR(" +
                            "[System.Runtime.InteropServices.Marshal]::SecureStringToBSTR(" +
                            $"(Get-Content \"{passwordFile}\" | ConvertTo-SecureString)))"));
                    if (res != null && res.Count > 0) return res[0].ToString();
                }
                catch
                {
                    //  PowerShellで暗号化したパスワードファイルの読み込みに失敗した場合、平文テキストとして読み込み
                    //  複数行の場合、最初の1行のみをパスワードとして判断
                    using (var sr = new StreamReader(passwordFile, new UTF8Encoding(false)))
                    {
                        return sr.ReadLine();
                    }
                }
            }
            else if (string.IsNullOrEmpty(password) && (session?.IsPasswordEmpty() ?? true))
            {
                //  Password, PasswordFile, Credentialの全部が空の場合
                return ReadPassword();
            }
            return password;
        }

        /// <summary>
        /// パスワード入力
        /// 入力した文字はアスタリスクで表示
        /// </summary>
        /// <returns></returns>
        private string ReadPassword()
        {
            Console.Write("Password: ");

            var sb = new StringBuilder();
            ConsoleKeyInfo key;
            int startTop = Console.CursorTop;
            int startLeft = Console.CursorLeft;

            while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
            {
                switch (key.Key)
                {
                    case ConsoleKey.Backspace:
                        if (sb.Length > 0)
                        {
                            sb.Remove(sb.Length - 1, 1);

                            if (Console.CursorLeft == 0)
                            {
                                Console.SetCursorPosition(Console.BufferWidth - 1, Console.CursorTop - 1);
                                Console.Write(' ');
                                Console.SetCursorPosition(Console.BufferWidth - 1, Console.CursorTop);
                            }
                            else
                            {
                                Console.Write("\b \b");
                            }
                        }
                        break;
                    case ConsoleKey.Tab:
                        break;
                    case ConsoleKey.Escape:
                        Console.SetCursorPosition(0, Console.CursorTop);
                        while (Console.CursorTop > startTop)
                        {
                            Console.Write(new string(' ', Console.WindowWidth));
                            Console.SetCursorPosition(0, Console.CursorTop - 1);
                        }
                        Console.Write(new string(' ', Console.WindowWidth));
                        Console.SetCursorPosition(0, Console.CursorTop);
                        Console.Write("Password: ");
                        sb.Clear();
                        break;
                    default:
                        sb.Append(key.KeyChar);
                        Console.Write("*");
                        break;
                }
            }

            Console.WriteLine();
            return sb.ToString();
        }

        #endregion

        /// <summary>
        /// 改行コード判定用正規表現
        /// </summary>
        protected readonly Regex pattern_return = new Regex(@"\r?\n");

        /// <summary>
        /// リモートパスに環境変数を含んだパスであるかどうかを判定する為の文字。
        /// Windows用⇒%、Linux/Mac用⇒~, $
        /// </summary>
        protected char[] candidate_envChar = new[] { '%', '~', '$' };

        protected PSSsh.Lib.Platform CheckRemotePlatform(SshSession session)
        {
            var client = session.CreateAndConnectSshClient();
            SshCommand command = client.CreateCommand($"uname");
            command.Execute();

            if (string.IsNullOrEmpty(command.Result))
            {
                //  unameコマンド実行失敗の為、Windowsと仮定
                command = client.CreateCommand($"ver");
                command.Execute();
            }
            List<string> splitResult = pattern_return.Split(command.Result).ToList();
            string result = splitResult.Count > 0 ? splitResult[0] : null;

            return result switch
            {
                string w when w.StartsWith("Microsoft Windows") => PSSsh.Lib.Platform.Windows,
                "Linux" => PSSsh.Lib.Platform.Linux,
                "Darwin" => PSSsh.Lib.Platform.Mac,
                _ => PSSsh.Lib.Platform.Unknown
            };
        }
    }
}
