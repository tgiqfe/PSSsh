using Renci.SshNet;
using Renci.SshNet.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace PSSsh.Lib
{
    public class SshSession : IDisposable
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
            get { return "********"; }   //  パスワードを表示させない
            set { if (!string.IsNullOrEmpty(value)) _password = value; }
        }
        public bool KeyboardInteractive { get; set; }

        #endregion

        public bool Effemeral { get; set; }

        private SshClient _ssh = null;
        private ScpClient _scp = null;
        private SftpClient _sftp = null;

        const int _timeout = 10;
        private bool _isOpen = false;
        ConnectionInfo _connectionInfo = null;

        //  [案]公開鍵を使用してのSSH接続の実装も検討要

        public void Open()
        {
            if (_isOpen) { return; }

            var info = new ServerInfo(_server, defaultPort: _port ?? 22, defaultProtocol: "ssh");
            _connectionInfo = GetConnectionInfo(info.Server, info.Port, _user, _password, KeyboardInteractive);
            _isOpen = true;
        }

        private ConnectionInfo GetConnectionInfo(string server, int port, string user, string password, bool keyboardInteractive)
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

        public bool IsUserEmpty()
        {
            return string.IsNullOrEmpty(_user);
        }

        public bool IsPasswordEmpty()
        {
            return string.IsNullOrEmpty(_password);
        }

        private static readonly Regex pattern_return = new Regex(@"\r?\n");

        /// <summary>
        /// リモートSSHコマンドを実行し、最初の1行だけを返す。
        /// </summary>
        /// <param name="commandText"></param>
        /// <returns></returns>
        public string ExecCommandOneLine(string commandText)
        {
            var client = CreateAndConnectSshClient();
            SshCommand command = client.CreateCommand(commandText);
            command.Execute();

            return pattern_return.Split(command.Result).DefinedFirst();
        }

        /// <summary>
        /// リモートSSHコマンドを実行して、接続先のOS種別を返す
        /// </summary>
        /// <returns></returns>
        public PSSsh.Lib.Platform CheckRemotePlatform()
        {
            var client = CreateAndConnectSshClient();
            SshCommand command = client.CreateCommand("uname");
            command.Execute();

            if (string.IsNullOrEmpty(command.Result))
            {
                //  unameコマンド実行失敗の為、Windows用コマンドを実行
                command = client.CreateCommand($"ver");
                command.Execute();
            }

            return pattern_return.Split(command.Result).DefinedFirst() switch
            {
                string w when w.StartsWith("Microsoft Windows") => PSSsh.Lib.Platform.Windows,
                "Linux" => PSSsh.Lib.Platform.Linux,
                "Darwin" => PSSsh.Lib.Platform.Mac,
                _ => PSSsh.Lib.Platform.Unknown
            };
        }

        #region Create client

        /// <summary>
        /// SSH接続用クライアントを取得
        /// </summary>
        /// <returns></returns>
        public SshClient CreateAndConnectSshClient()
        {
            this._ssh ??= CreateAndConnectClient<SshClient>();
            return _ssh;
        }

        /// <summary>
        /// SCP接続用クライアントを取得
        /// </summary>
        /// <returns></returns>
        public ScpClient CreateAndConnectScpClient()
        {
            this._scp ??= CreateAndConnectClient<ScpClient>();
            return _scp;
        }

        /// <summary>
        /// SFTP接続用クライアントを取得
        /// </summary>
        /// <returns></returns>
        public SftpClient CreateAndConnectSftpClient()
        {
            this._sftp ??= CreateAndConnectClient<SftpClient>();
            return _sftp;
        }

        private T CreateAndConnectClient<T>() where T : BaseClient
        {
            Open();
            T client = (T)typeof(T).GetConstructor(new Type[] { typeof(ConnectionInfo) }).Invoke(new object[1] { _connectionInfo });
            client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(_timeout);

            try
            {
                client.Connect();
            }
            catch (Renci.SshNet.Common.SshAuthenticationException sae)
            {
                Console.WriteLine("認証失敗");
                Console.WriteLine(sae);
            }
            return client;
        }


        #endregion

        ~SshSession()
        {
            Close();
        }

        public void CloseIfEffemeral()
        {
            if (this.Effemeral) Close();
        }

        public void Close()
        {
            if (_ssh != null) _ssh.Dispose();
            if (_scp != null) _scp.Dispose();
            if (_sftp != null) _sftp.Dispose();
        }

        #region Dispose

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Close();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
