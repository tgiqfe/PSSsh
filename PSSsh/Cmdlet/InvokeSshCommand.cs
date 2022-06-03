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
    internal class InvokeSshCommand : PSCmdletExtension
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
        public string[] Command { get; set; }

        [Parameter]
        public string CommandFile { get; set; }

        [Parameter, Alias("Output")]
        public string OutputFile { get; set; }

        #endregion

        readonly Regex pattern_return = new Regex(@"\r?\n");

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            this.User = GetUserName(this.User, this.Credential);
            this.Password = GetPassword(this.Password, this.Credential, this.PasswordFile);
        }

        protected override void ProcessRecord()
        {
            if ((this.Command == null || this.Command.Length == 0) &&
                !string.IsNullOrEmpty(this.CommandFile) && File.Exists(this.CommandFile))
            {
                string text = File.ReadAllText(this.CommandFile);
                this.Command = pattern_return.Split(text);
            }

            var info = new ServerInfo(this.Server, defaultPort: this.Port ?? 22, defaultProtocol: "ssh");
            var connectionInfo = GetConnectionInfo(info.Server, info.Port, this.User, this.Password, KeyboardInteractive);
            try
            {
                using (var client = new SshClient(connectionInfo))
                {
                    client.ConnectionInfo.Timeout = TimeSpan.FromSeconds(CONNECT_TIMEOUT);
                    client.Connect();

                    foreach (string line in Command)
                    {
                        SshCommand command = client.CreateCommand(line);
                        command.Execute();

                        List<string> splitResult = pattern_return.Split(command.Result).ToList();
                        splitResult.RemoveAt(0);
                        splitResult.RemoveAt(splitResult.Count - 1);
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
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
