using Renci.SshNet;
using System.Diagnostics.Contracts;
using System.Management.Automation;
using System.Text.RegularExpressions;

namespace PSSsh.Cmdlet
{
    [Cmdlet(VerbsLifecycle.Invoke, "SshCommand")]
    public class InvokeSshCommand : PSCmdletExtension
    {
        #region Command Parameters

        [Parameter(Position = 0)]
        public string Server { get; set; }

        [Parameter]
        public int? Port { get; set; }

        [Parameter]
        public string User { get; set; }

        [Parameter]
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

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

        }

        protected override void ProcessRecord()
        {
            if ((this.Command == null || this.Command.Length == 0)
                && !string.IsNullOrEmpty(this.CommandFile) && File.Exists(this.CommandFile))
            {
                var text = File.ReadAllText(this.CommandFile);
                this.Command = new Regex(@"\r?\n").
                   Split(text).
                   Where(line => !string.IsNullOrWhiteSpace(line)).
                   ToArray();
            }

            ConnectionInfo info = new ConnectionInfo(
                this.Server,
                this.Port ?? 22,
                this.User,
                new PasswordAuthenticationMethod(this.User, this.Password)
                );

            using (SshClient ssh = new SshClient(info))
            {
                ssh.Connect();
                if (!ssh.IsConnected)
                {
                    WriteError(new ErrorRecord(new Exception("Failed to connect to the server."), "ConnectionFailed", ErrorCategory.ConnectionError, this.Server));
                    return;
                }

                SshCommand cmd = ssh.CreateCommand(string.Join(" && ", this.Command));
                cmd.Execute();

                var stdOut = cmd.Result;
                var stdErr = cmd.Error;
                if (stdOut != null)
                {
                    WriteObject(stdOut);
                }
                if (cmd.ExitStatus != 0 && stdErr != null)
                {
                    WriteError(new ErrorRecord(new Exception(stdErr), "CommandExecutionFailed", ErrorCategory.InvalidOperation, this.Command));
                }
            }
        }
    }
}