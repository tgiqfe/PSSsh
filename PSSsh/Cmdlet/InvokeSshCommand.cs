using Renci.SshNet;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Text;
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

        [Parameter]
        public string OutputFile { get; set; }

        [Parameter]
        public string OutputDirectory { get; set; }

        [Parameter]
        public SwitchParameter Sudo { get; set; }

        [Parameter]
        public int TimeoutSeconds { get; set; } = 7200;

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
            using (var client = new SshClient(info))
            {
                client.Connect();
                if (!client.IsConnected)
                {
                    WriteError(new ErrorRecord(new Exception("Failed to connect to the SSH server."), "ConnectionFailed", ErrorCategory.ConnectionError, null));
                    return;
                }

                if (this.Sudo)
                {
                    using (var shell = client.CreateShellStream("terminal", 80, 24, 800, 600, 1024))
                    {
                        shell.DataReceived += (sender, e) => { };
                        var output = new StringBuilder();

                        // 初期プロンプトを待つ
                        Thread.Sleep(500);
                        string initialPrompt = shell.Read();

                        foreach (var command in this.Command)
                        {
                            shell.WriteLine($"sudo -S {command}");
                            Thread.Sleep(500);

                            // パスワードプロンプトを検出（タイムアウト付き）
                            var promptDetected = false;
                            var promptOutput = new StringBuilder();
                            var startTime = DateTime.Now;

                            while ((DateTime.Now - startTime).TotalSeconds < 5)
                            {
                                if (shell.DataAvailable)
                                {
                                    var data = shell.Read();
                                    promptOutput.Append(data);

                                    // パスワードプロンプトを検出
                                    if (Regex.IsMatch(data, @"(password:|Password:|\[sudo\] password for)", RegexOptions.IgnoreCase))
                                    {
                                        promptDetected = true;
                                        break;
                                    }
                                }
                                Thread.Sleep(100);
                            }

                            // パスワードを送信
                            if (promptDetected)
                            {
                                shell.WriteLine(this.Password);
                                Thread.Sleep(500);
                            }

                            // コマンド結果を読み取る（プロンプト検出方式）
                            var resultOutput = new StringBuilder();
                            var commandStartTime = DateTime.Now;
                            var maxTimeout = TimeSpan.FromSeconds(this.TimeoutSeconds);

                            while (true)
                            {
                                // 接続切断を検出（シャットダウンコマンドなど）
                                if (!client.IsConnected)
                                {
                                    break;
                                }

                                if (shell.DataAvailable)
                                {
                                    var data = shell.Read();
                                    resultOutput.Append(data);

                                    // シェルプロンプトを検出（コマンド完了）
                                    if (Regex.IsMatch(data, @"^[^@]+@.*[%\$#>]\s*", RegexOptions.Multiline))
                                    {
                                        break;
                                    }
                                }
                                else if ((DateTime.Now - commandStartTime) > maxTimeout)
                                {
                                    // タイムアウト
                                    WriteError(new ErrorRecord(
                                        new Exception($"Command execution timed out after {this.TimeoutSeconds} seconds."),
                                        "CommandTimeout",
                                        ErrorCategory.OperationTimeout,
                                        info.Host));
                                    break;
                                }
                                Thread.Sleep(100);
                            }

                            output.Append(resultOutput.ToString());
                        }

                        var finalOutput = CleanOutput(output.ToString());
                        SaveOutput(info.Host, finalOutput);

                        WriteObject(finalOutput);
                    }
                }
                else
                {
                    using (var ssh = new SshClient(info))
                    {
                        var output = new StringBuilder();
                        ssh.Connect();
                        foreach (var command in this.Command)
                        {
                            var cmd = ssh.CreateCommand(command);
                            cmd.CommandTimeout = TimeSpan.FromSeconds(this.TimeoutSeconds);
                            cmd.Execute();

                            output.AppendLine(cmd.Result);
                            output.AppendLine(cmd.Error);
                        }

                        string outputText = output.ToString();
                        SaveOutput(info.Host, outputText);

                        WriteObject(outputText);
                    }
                }

                client.Disconnect();
            }
        }

        private void SaveOutput(string host, string output)
        {
            if (!string.IsNullOrEmpty(this.OutputFile))
            {
                File.AppendAllText(this.OutputFile, $"[{host}]\n{output}\n");
            }
            else if (!string.IsNullOrEmpty(this.OutputDirectory))
            {
                var fileName = $"{host}_{DateTime.Now:yyyyMMddHHmmss}.txt";
                var filePath = Path.Combine(this.OutputDirectory, fileName);
                File.WriteAllText(filePath, output);
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
                    Regex.IsMatch(line, @"^[^@]+@.*[%\$#>]\s*") ||
                    Regex.IsMatch(line, @"^\s*%\s*$"))
                {
                    continue;
                }

                var isEmpty = string.IsNullOrWhiteSpace(line);

                // 最初の空行をスキップ
                if (!hasStarted && isEmpty) continue;

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
