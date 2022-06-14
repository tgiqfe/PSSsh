using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using PSSsh.Lib;
using Renci.SshNet;
using System.Text.RegularExpressions;

namespace PSSsh.Cmdlet
{
    /// <summary>
    /// SSHコマンド実行用コマンドレット
    /// </summary>
    [Cmdlet(VerbsLifecycle.Invoke, "SshCommand")]
    public class InvokeSshCommand : PSCmdletExtension
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

        [Parameter]
        public string[] Command { get; set; }

        [Parameter]
        public string CommandFile { get; set; }

        [Parameter, Alias("Output")]
        public string OutputFile { get; set; }

        #endregion

        private readonly Regex pattern_return = new Regex(@"\r?\n");

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            this.User = GetUserName(this.User, this.Credential, this.Session);
            this.Password = GetPassword(this.Password, this.Credential, this.PasswordFile, this.Session);
        }

        protected override void ProcessRecord()
        {
            //  Commandファイルから読み取り
            if ((this.Command == null || this.Command.Length == 0) &&
                !string.IsNullOrEmpty(this.CommandFile) && File.Exists(this.CommandFile))
            {
                string text = File.ReadAllText(this.CommandFile);
                this.Command = pattern_return.Split(text);
            }

            this.Session ??= new SshSession()
            {
                Server = this.Server,
                Port = this.Port,
                User = this.User,
                Password = this.Password,
                KeyboardInteractive = this.KeyboardInteractive,
                Effemeral = true,   //  コマンドパラメータでSession指定が無い場合、Effemeral。
            };

            var client = Session.CreateAndConnectSshClient();
            if (client.IsConnected)
            {
                foreach (string line in Command)
                {
                    SshCommand command = client.CreateCommand(line);
                    command.Execute();

                    /*
                    List<string> splitResult = pattern_return.Split(command.Result).ToList();

                    if (splitResult.Count > 0 && string.IsNullOrEmpty(splitResult[0]))
                    {
                        splitResult.RemoveAt(0);
                    }
                    if (splitResult.Count > 0 && string.IsNullOrEmpty(splitResult[splitResult.Count - 1]))
                    {
                        splitResult.RemoveAt(splitResult.Count - 1);
                    }
                    */
                    var splitResult = pattern_return.Split(command.Result).Trim();

                    if (string.IsNullOrEmpty(this.OutputFile))
                    {
                        WriteObject(string.Join("\r\n", splitResult), true);
                    }
                    else
                    {
                        TargetDirectory.CreateParent(this.OutputFile);
                        using (var sw = new StreamWriter(OutputFile, true, new UTF8Encoding(false)))
                        {
                            sw.Write(string.Join("\r\n", splitResult));
                        }
                    }
                }
            }
            Session.CloseIfEffemeral();
        }
    }
}
