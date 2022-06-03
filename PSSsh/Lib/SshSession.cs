using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSSsh.Lib
{
    internal class SshSession
    {
        #region SSH Connect parameter

        private string _server = null;
        private int? _port = null;
        private string _user = null;
        private string _password = null;

        public string Server
        {
            get { return _server; }
            set { if (!string.IsNullOrEmpty(value)) _server = value; }
        }
        public int? Port
        {
            get { return _port; }
            set { if (value != null) _port = value; }
        }
        public string User
        {
            get { return _user; }
            set { if (!string.IsNullOrEmpty(value)) _user = value; }
        }
        public string Password
        {
            get { return _password; }
            set { if (!string.IsNullOrEmpty(value)) _password = value; }
        }
        public bool KeyboardInteractive { get; set; }

        #endregion

        private bool _isOpen = false;
        ConnectionInfo _connectionInfo = null;

        public void Open()
        {
            if (_isOpen) { return; }

            var info = new ServerInfo(this.Server, defaultPort: this.Port ?? 22, defaultProtocol: "ssh");
            _connectionInfo = getConnectionInfo(info.Server, info.Port, this.User, this.Password, KeyboardInteractive);
            _isOpen = true;

            ConnectionInfo getConnectionInfo(string server, int port, string user, string password, bool keyboardInteractive)
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

        public SshClient CreateSshClient()
        {
            if (!_isOpen)
            {
                Open();
            }
            return new SshClient(_connectionInfo);
        }

        public ScpClient CreateScpClient()
        {
            return new ScpClient(_connectionInfo);
        }

        public SftpClient CreateSftpClient()
        {
            return new SftpClient(_connectionInfo);
        }
    }
}
