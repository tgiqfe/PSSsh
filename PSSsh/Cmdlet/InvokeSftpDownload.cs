﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using PSSsh.Lib;
using Renci.SshNet;

namespace PSSsh.Cmdlet
{
    /// <summary>
    /// SFTPダウンロード用コマンドレット
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "SftpDownload")]
    internal class InvokeSftpDownload : PSCmdletExtension
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

        [Parameter(Mandatory = true)]
        public string RemotePath { get; set; }

        [Parameter(Mandatory = true)]
        public string LocalPath { get; set; }

        #endregion

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            this.User = GetUserName(this.User, this.Credential);
            this.Password = GetPassword(this.Password, this.Credential, this.PasswordFile);
        }

        protected override void ProcessRecord()
        {
            var info = new ServerInfo(this.Server, defaultPort: this.Port ?? 22, defaultProtocol: "ssh");
            var connectionInfo = GetConnectionInfo(info.Server, info.Port, this.User, this.Password, KeyboardInteractive);
            try
            {
                TargetDirectory.CreateParent(this.LocalPath);
                using (var client = new SftpClient(connectionInfo))
                using (var fs = File.OpenWrite(LocalPath))
                {
                    client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(CONNECT_TIMEOUT);
                    client.Connect();
                    client.DownloadFile(RemotePath, fs);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
