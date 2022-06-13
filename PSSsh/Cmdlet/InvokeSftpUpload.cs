using System;
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
    /// SFTPアップロード用コマンドレット
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "SftpUpload")]
    public class InvokeSftpUpload : PSCmdletExtension
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

        readonly System.Text.RegularExpressions.Regex pattern_return =
            new System.Text.RegularExpressions.Regex(@"\r?\n");

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
            if (RemotePath.Contains("~") || RemotePath.Contains("%") || RemotePath.Contains("$"))
            {
                var tempClient = Session.CreateAndConnectSshClient();
                SshCommand tempCommand = tempClient.CreateCommand($"echo {RemotePath}");
                tempCommand.Execute();

                List<string> splitResult = pattern_return.Split(tempCommand.Result).ToList();
                RemotePath = splitResult.Count > 0 ? splitResult[0] : null;
            }

            var client = Session.CreateAndConnectSftpClient();
            if (client.IsConnected)
            {
                using (var fs = File.OpenWrite(LocalPath))
                {
                    client.UploadFile(fs, RemotePath);
                }
            }
            Session.CloseIfEffemeral();
        }
    }
}
