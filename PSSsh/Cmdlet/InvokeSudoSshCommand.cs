using Renci.SshNet;
using System.Management.Automation;
using System.Text;
using System.Text.RegularExpressions;

namespace PSSsh.Cmdlet
{
    [Cmdlet(VerbsLifecycle.Invoke, "RootSshCommand")]
    public class InvokeRootSshCommand : PSCmdletExtension
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
                new PasswordAuthenticationMethod(this.User, this.Password));

            using (var client = new SshClient(info))
            {
                client.Connect();
                if (!client.IsConnected)
                {
                    WriteError(new ErrorRecord(new Exception("Failed to connect to the SSH server."), "ConnectionFailed", ErrorCategory.ConnectionError, null));
                    return;
                }

                using (var shell = client.CreateShellStream("terminal", 80, 24, 800, 600, 1024))
                {
                    var output = new StringBuilder();
                    
                    foreach (var command in this.Command)
                    {
                        shell.WriteLine($"sudo -S {command}");
                        Thread.Sleep(500);

                        var prompt = shell.Expect(
                            new Regex(@"(\[sudo\] password for .+:|password:)", RegexOptions.IgnoreCase), TimeSpan.FromSeconds(2));
                        if (!string.IsNullOrEmpty(prompt))
                        {
                            shell.WriteLine(this.Password);
                            Thread.Sleep(500);
                        }

                        var result = shell.Expect(new Regex(@"[\$#>]\s*$"), TimeSpan.FromSeconds(5));
                        output.AppendLine(result);
                    }

                    var finalOutput = CleanOutput(output.ToString());
                    WriteObject(finalOutput);
                }
                client.Disconnect();
            }
        }

        private string CleanOutput(string output)
        {
            var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
            var cleanedLines = new List<string>();
            var hasStarted = false;
            var emptyLineCount = 0;

            foreach (var line in lines)
            {
                // パスワードプロンプト、sudoコマンドエコー、シェルプロンプトを除外
                if (line.Contains("[sudo] password for") ||
                    line.Contains("sudo -S") ||
                    Regex.IsMatch(line, @"^[^@]+@[^:]+:.*[\$#>]\s*$"))
                {
                    continue;
                }

                var isEmpty = string.IsNullOrWhiteSpace(line);

                // 最初の空行をスキップ
                if (!hasStarted && isEmpty)
                {
                    continue;
                }

                if (isEmpty)
                {
                    emptyLineCount++;
                }
                else
                {
                    hasStarted = true;

                    // 空行が2行以上連続していた場合のみ1行の空行を追加
                    if (emptyLineCount >= 2)
                    {
                        cleanedLines.Add(string.Empty);
                    }

                    emptyLineCount = 0;
                    cleanedLines.Add(line);
                }
            }

            // 最後の空行を削除
            while (cleanedLines.Count > 0 && string.IsNullOrWhiteSpace(cleanedLines[cleanedLines.Count - 1]))
            {
                cleanedLines.RemoveAt(cleanedLines.Count - 1);
            }

            return string.Join(Environment.NewLine, cleanedLines);
        }
    }
}
