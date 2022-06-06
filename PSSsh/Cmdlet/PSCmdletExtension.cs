﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using Renci.SshNet;
using Renci.SshNet.Common;

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

        protected string GetUserName(string user, PSCredential credential)
        {
            if (credential != null)
            {
                return credential.UserName;
            }
            if (string.IsNullOrEmpty(user))
            {
                Console.Write("User: ");
                user = Console.ReadLine();
                if (string.IsNullOrEmpty(user))
                {
                    return Environment.UserName;
                }
            }
            return user;
        }

        protected string GetPassword(string password, PSCredential credential, string passwordFile)
        {
            if (credential != null)
            {
                return System.Runtime.InteropServices.Marshal.PtrToStringUni(
                    System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(credential.Password));
            }
            else if (!string.IsNullOrEmpty(passwordFile) && File.Exists(passwordFile))
            {
                var res = InvokeCommand.InvokeScript(
                    SessionState,
                    InvokeCommand.NewScriptBlock(
                        "[System.Runtime.InteropServices.Marshal]::PtrToStringBSTR(" +
                        "[System.Runtime.InteropServices.Marshal]::SecureStringToBSTR(" +
                        $"(Get-Content \"{passwordFile}\" | ConvertTo-SecureString)))"));
                if (res != null && res.Count > 0) return res[0].ToString();
            }
            else if(string.IsNullOrEmpty(password))
            {
                Console.Write("Password: ");
                password = Console.ReadLine();

                //  [案]Password, PasswordFile, Credentialの全部が空っぽだった場合、対話でパスワード入力を。

            }
            return password;
        }

        //  ↓削除予定メソッド (移行済み)
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