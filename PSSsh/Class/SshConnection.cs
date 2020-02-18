using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Renci.SshNet;
using Renci.SshNet.Common;
using System.Management.Automation;

namespace PSSsh
{
    internal class SshConnection
    {
        /// <summary>
        /// ConnectionInfoを取得する為の静的メソッド
        /// </summary>
        /// <param name="server"></param>
        /// <param name="port"></param>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="keyboardInteractive"></param>
        /// <returns></returns>
        public static ConnectionInfo GetConnectionInfo(string server, int port, string user, string password, bool keyboardInteractive)
        {
            if (keyboardInteractive)
            {
                KeyboardInteractiveAuthenticationMethod keyAuth = new KeyboardInteractiveAuthenticationMethod(user);
                keyAuth.AuthenticationPrompt += new EventHandler<AuthenticationPromptEventArgs>((sender, e) =>
                {
                    foreach (AuthenticationPrompt prompt in e.Prompts)
                    {
                        if (prompt.Request.StartsWith("Password:", StringComparison.OrdinalIgnoreCase))
                        {
                            prompt.Response = password;
                        }
                        if (prompt.Request.StartsWith("Verification code:", StringComparison.OrdinalIgnoreCase))
                        {
                            /* ワンタイムパスワード用 (ごめん未実装) */
                        }
                    }
                });
                return new ConnectionInfo(server, port, user, keyAuth);
            }
            else
            {
                return new ConnectionInfo(
                   server,
                   port,
                   user,
                   new AuthenticationMethod[] { new PasswordAuthenticationMethod(user, password) });
            }
        }
    }
}
