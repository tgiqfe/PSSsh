﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using Renci.SshNet;
using Renci.SshNet.Common;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;

namespace PSSsh.Cmdlet
{
    /// <summary>
    /// 対象のSSHサーバにSCPを使用してファイルをアップロード
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "ScpUpload")]
    public class InvokeScpUpload : PSCmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string Server { get; set; }
        [Parameter]
        public int Port { get; set; } = Item.DEFAULT_PORT;
        [Parameter(Position = 1)]
        public string User { get; set; }
        [Parameter(Position = 2)]
        [LogIgnore]
        public string Password { get; set; }
        [Parameter]
        [LogNotNull]
        public string PasswordFile { get; set; }
        [Parameter]
        [LogIgnore]
        public PSCredential Credential { get; set; }
        [Parameter]
        public SwitchParameter KeyboardInteractive { get; set; }
        [Parameter]
        [LogNotNull]
        public SwitchParameter DebugMode { get; set; }

        [Parameter(Mandatory = true)]
        public string RemotePath { get; set; }
        [Parameter(Mandatory = true)]
        public string LocalPath { get; set; }

        protected override void BeginProcessing()
        {
            //  カレントディレクトリカレントディレクトリの一時変更
            Item.CurrentDirectory = Environment.CurrentDirectory;
            Environment.CurrentDirectory = this.SessionState.Path.CurrentFileSystemLocation.Path;

            if (Credential != null)
            {
                User = Credential.UserName;
                Password = Marshal.PtrToStringUni(Marshal.SecureStringToGlobalAllocUnicode(Credential.Password));
            }
            else if (!string.IsNullOrEmpty(PasswordFile) && File.Exists(PasswordFile))
            {
                Collection<PSObject> invokeResult = InvokeCommand.InvokeScript(
                    SessionState,
                    InvokeCommand.NewScriptBlock(string.Format(
                        "[System.Runtime.InteropServices.Marshal]::PtrToStringBSTR(" +
                        "[System.Runtime.InteropServices.Marshal]::SecureStringToBSTR(" +
                        "(Get-Content \"{0}\" | ConvertTo-SecureString)))", PasswordFile)));
                if (invokeResult != null && invokeResult.Count > 0)
                {
                    Password = invokeResult[0].ToString();
                }
            }

            bool debugMode = DebugMode;
#if DEBUG
            debugMode = true;
#endif
            Item.Logger = Function.SetLogger(Item.LOG_DIRECTORY, "InvokeScpUpload", debugMode);
            Item.Logger.Info(Function.GetPropertyString<InvokeScpUpload>(this));
        }

        protected override void ProcessRecord()
        {
            //ConnectionInfo info = new SshConnection(Server, Port, User, Password, KeyboardInteractive).GetConnectionInfo();
            ConnectionInfo info = SshConnection.GetConnectionInfo(Server, Port, User, Password, KeyboardInteractive);

            try
            {
                //  ↓Uploadがうまくいかない
                using (ScpClient scp = new ScpClient(info))
                {
                    scp.RemotePathTransformation = RemotePathTransformation.ShellQuote;
                    scp.ConnectionInfo.Timeout = TimeSpan.FromSeconds(Item.CONNECT_TIMEOUT_SECOND);
                    scp.Connect();
                    scp.Upload(new FileInfo(LocalPath), RemotePath);
                }
            }
            catch (Exception e)
            {
                Item.Logger.Error(e.ToString());
            }
        }

        protected override void EndProcessing()
        {
            //  カレントディレクトリを戻す
            Environment.CurrentDirectory = Item.CurrentDirectory;

            //  Loggerを終了
            Item.Logger = null;
        }
    }
}
