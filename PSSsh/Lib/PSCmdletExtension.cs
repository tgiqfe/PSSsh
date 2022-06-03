using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace PSSsh.Lib
{
    internal class PSCmdletExtension : PSCmdlet
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
            Environment.CurrentDirectory = this.SessionState.Path.CurrentFileSystemLocation.Path;
        }

        protected override void EndProcessing()
        {
            //  カレントディレクトリを戻す
            Environment.CurrentDirectory = _currentDirectory;
        }

        #endregion

        protected ConnectionInfo GetConnectionInfo(string server, int port, string user, string password, bool keyboardInteractive)
        {
            if (keyboardInteractive)
            {
                var keyAuth = new KeyboardInteractiveAuthenticationMethod(user);
                keyAuth.AuthenticationPrompt += new EventHandler<AuthenticationPromptEventArgs>((sender, e) =>
                {
                    foreach (var prompt in e.Prompts)
                    {
                        if (prompt.Request.StartsWith("Password:", StringComparison.OrdinalIgnoreCase))
                        {
                            prompt.Response = password;
                        }
                        if (prompt.Request.StartsWith("Verification code:", StringComparison.OrdinalIgnoreCase))
                        {
                            //  [案]ワンタイムパスワードを入力し、失敗した終了
                            //  ワンタイムパスワード用
                        }

                    }
                });
                return new ConnectionInfo(server, port, user, keyAuth);
            }
            else
            {
                return new ConnectionInfo(server, port, user, new AuthenticationMethod[] { new PasswordAuthenticationMethod(user, password) });
            }
        }
    }
}
