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
        /*
        public string Server { get; set; }
        public int Port { get; set; } = Item.DEFAULT_PORT;
        public string User { get; set; }
        public string Password { get; set; }
        public bool KeyboardInteractive { get; set; }

        public SshConnection() { }
        public SshConnection(string server, int port, string user, string password, bool keyboardInteractive)
        {
            if (server.Contains(":"))
            {
                this.Server = server.Substring(0, server.IndexOf(":"));
                this.Port = int.TryParse(server.Substring(server.IndexOf(":") + 1), out int tempInt) ? tempInt : Item.DEFAULT_PORT;
            }
            else
            {
                this.Server = server;
                this.Port = port > 0 && port < 65536 ? port : Item.DEFAULT_PORT;
            }
            this.User = user;
            this.Password = password;
            this.KeyboardInteractive = keyboardInteractive;
        }

        /// <summary>
        /// SSH接続用 ConnectionInfo インスタンスを生成
        /// </summary>
        /// <returns></returns>
        public ConnectionInfo GetConnectionInfo()
        {
            if (KeyboardInteractive)
            {
                KeyboardInteractiveAuthenticationMethod keyAuth = new KeyboardInteractiveAuthenticationMethod(User);
                keyAuth.AuthenticationPrompt += new EventHandler<AuthenticationPromptEventArgs>((sender, e) =>
                {
                    foreach (AuthenticationPrompt prompt in e.Prompts)
                    {
                        if (prompt.Request.StartsWith("Password:", StringComparison.OrdinalIgnoreCase))
                        {
                            prompt.Response = Password;
                        }
                        if (prompt.Request.StartsWith("Verification code:", StringComparison.OrdinalIgnoreCase))
                        {
                            // ワンタイムパスワード用 (ごめん未実装)
                        }
                    }
                });
                return new ConnectionInfo(Server, Port, User, keyAuth);
            }
            else
            {
                return new ConnectionInfo(
                   Server,
                   Port,
                   User,
                   new AuthenticationMethod[] { new PasswordAuthenticationMethod(User, Password) });
            }
        }
        */

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
