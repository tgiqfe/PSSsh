using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using Renci.SshNet;
using Renci.SshNet.Common;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Collections.ObjectModel;

namespace PSSsh.Cmdlet
{
    /// <summary>
    /// 対象のSSHサーバ宛にSSH接続してコマンドを実行
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "SshCommand")]
    public class InvokeSshCommand : PSCmdlet
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

        [Parameter(Mandatory = true)]
        public string[] Command { get; set; }
        [Parameter]
        public string Output { get; set; }

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

            bool debugMode = false;
#if DEBUG
            debugMode = true;
#endif
            Item.Logger = Function.SetLogger(Item.LOG_DIRECTORY, "InvokeSshCommand", debugMode);
            Item.Logger.Info(Function.GetPropertyString<InvokeSshCommand>(this));
        }

        protected override void ProcessRecord()
        {
            //ConnectionInfo info = new SshConnection(Server, Port, User, Password, KeyboardInteractive).GetConnectionInfo();
            ConnectionInfo info = SshConnection.GetConnectionInfo(Server, Port, User, Password, KeyboardInteractive);

            try
            {
                using (SshClient ssh = new SshClient(info))
                {
                    ssh.ConnectionInfo.Timeout = TimeSpan.FromSeconds(Item.CONNECT_TIMEOUT_SECOND);
                    ssh.Connect();

                    foreach (string cmd in Command)
                    {
                        SshCommand command = ssh.CreateCommand(cmd);
                        command.Execute();

                        if (string.IsNullOrEmpty(Output))
                        {
                            WriteObject(command.Result, true);
                        }
                        else
                        {
                            if (Output.Contains("\\"))
                            {
                                if (!Directory.Exists(Path.GetDirectoryName(Output)))
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(Output));
                                }
                            }
                            using (StreamWriter sw = new StreamWriter(Output, true, Encoding.GetEncoding("Shift_JIS")))
                            {
                                sw.Write(command.Result);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Item.Logger.Error(e.ToString());
            }
        }

        //  検討中
        //  RunAsyncオプション時に使用できそうな処理なので、メモとして残す
        /*
        private async Task ExecuteAsync(SshCommand cmd)
        {
            var asyncResult = cmd.BeginExecute();

            StreamReader stdout = new StreamReader(cmd.OutputStream);
            StreamReader stderr = new StreamReader(cmd.ExtendedOutputStream);
            while (!asyncResult.IsCompleted)
            {
                await Task.Run(async () =>
                {
                    string outLine = await stdout.ReadToEndAsync();
                    if (!string.IsNullOrEmpty(outLine)) { Console.WriteLine(outLine); }
                });
                await Task.Run(async () =>
                {
                    string errLine = await stderr.ReadToEndAsync();
                    if (!string.IsNullOrEmpty(errLine)) { Console.WriteLine(errLine); }
                });
            }

            cmd.EndExecute(asyncResult);
        }
        */

        //  検討中
        //  ShellStreamは、後日追加予定の Enter-SshSession で使用予定
        /*
        private void ExecuteSSH(SshClient ssh, string[] cmdList)
        {
            using (ShellStream ss = ssh.CreateShellStream("ssh", 180, 324, 1800, 3600, 8000))
            {
                foreach (string command in cmdList)
                {
                    ss.Write(command + "\n");
                    Thread.Sleep(500);
                    string tempStr = ss.Read();
                    if (!string.IsNullOrEmpty(tempStr))
                    {
                        Console.WriteLine(tempStr);
                    }
                }
            }
        }
        */

        protected override void EndProcessing()
        {
            //  カレントディレクトリを戻す
            Environment.CurrentDirectory = Item.CurrentDirectory;

            //  Loggerを終了
            Item.Logger = null;
        }
    }
}
