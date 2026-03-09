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
        public string[] Server { get; set; }

        [Parameter]
        public int? Port { get; set; }

        [Parameter]
        public string User { get; set; }

        [Parameter]
        public string Password { get; set; }

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

        [Parameter]
        public SwitchParameter Sudo { get; set; }


        #endregion

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            if (this.Credential != null)
            {
                this.User = this.Credential.UserName;
                this.Password = this.Credential.GetNetworkCredential().Password;
            }
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

            var servers = this.Server.SelectMany(s => ExpandIpRange(s));
            foreach (var server in servers)
            {
                ConnectionInfo info = new ConnectionInfo(
                    server,
                    this.Port ?? 22,
                    this.User,
                    new PasswordAuthenticationMethod(this.User, this.Password));
                RunSshCommand(info);
            }
        }

        private string[] ExpandIpRange(string ipRange)
        {
            if (!ipRange.Contains('~')) return new[] { ipRange };

            var parts = ipRange.Split('~');
            if (parts.Length != 2) return new[] { ipRange };

            // 開始IPアドレスを解析
            var startIp = parts[0].Trim();
            var ipParts = startIp.Split('.');
            if (ipParts.Length != 4) return new[] { ipRange };

            // 終了値を取得
            if (!int.TryParse(parts[1].Trim(), out int endValue)) return new[] { ipRange };

            // 開始値を取得
            if (!int.TryParse(ipParts[3], out int startValue)) return new[] { ipRange };

            // IPアドレスのプレフィックス（最初の3オクテット）
            var prefix = $"{ipParts[0]}.{ipParts[1]}.{ipParts[2]}.";

            // 範囲のIPアドレスを生成
            var result = new List<string>();
            for (int i = startValue; i <= endValue; i++)
            {
                result.Add($"{prefix}{i}");
            }

            return result.ToArray();
        }

        private void RunSshCommand(ConnectionInfo info)
        {
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