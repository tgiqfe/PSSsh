﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using PSSsh.Lib;
using System.IO;
using Renci.SshNet;

namespace PSSsh.Cmdlet
{
    /// <summary>
    /// Scpダウンロード用コマンドレット
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "ScpDownload")]
    public class InvokeScpDownload : PSCmdletExtension
    {
        #region Command Parameter

        [Parameter(Position = 0)]
        public string Server { get; set; }

        [Parameter]
        public int? Port { get; set; }

        [Parameter(Position = 1)]
        public string User { get; set; }

        [Parameter(Position = 2)]
        public string Password { get; set; }

        [Parameter]
        public string PasswordFile { get; set; }

        [Parameter]
        public PSCredential Credential { get; set; }

        [Parameter]
        public SwitchParameter KeyboardInteractive { get; set; }

        [Parameter]
        public SshSession Session { get; set; }

        [Parameter(Mandatory = true), Alias("Remote")]
        public string RemotePath { get; set; }

        [Parameter(Mandatory = true), Alias("Local")]
        public string LocalPath { get; set; }

        #endregion

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            this.User = GetUserName(this.User, this.Credential, this.Session);
            this.Password = GetPassword(this.Password, this.Credential, this.PasswordFile, this.Session);
        }

        protected override void ProcessRecord()
        {
            this.Session ??= new SshSession()
            {
                Server = this.Server,
                Port = this.Port,
                User = this.User,
                Password = this.Password,
                KeyboardInteractive = this.KeyboardInteractive,
                Effemeral = true,
            };

            //  宛先に変数が含まれている場合、事前にSSHでコマンドを実行してパスを取得
            if (candidate_envChar.Any(x => RemotePath.Contains(x)))
            {
                this.RemotePath = Session.ExecCommandOneLine($"echo {RemotePath}");
            }

            //  ダウンロード先パスの末尾に「\\」「/」が含まれている場合、対象ディレクトリ配下パスに変更
            if (candidate_dirSeparator.Any(x => LocalPath.EndsWith(x)))
            {
                this.LocalPath = GetPathFromDirectory(RemotePath, LocalPath);
            }

            var client = Session.CreateAndConnectScpClient();
            if (client.IsConnected)
            {
                client.RemotePathTransformation = RemotePathTransformation.ShellQuote;
                client.Download(RemotePath, new FileInfo(LocalPath));
            }
            Session.CloseIfEffemeral();
        }
    }
}
