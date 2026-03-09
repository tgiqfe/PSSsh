using Renci.SshNet;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace PSSsh.Cmdlet
{
    [Cmdlet(VerbsLifecycle.Invoke, "MultiSshCommand")]
    public class InvokeMultiSshCommand : PSCmdletExtension
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
        public int MaxParallelThreads { get; set; } = 10;

        [Parameter]
        public int TimeoutSeconds { get; set; } = 300;

        #endregion

        private readonly object _writeLock = new object();
        private readonly ConcurrentBag<ErrorRecord> _errors = new ConcurrentBag<ErrorRecord>();
        private readonly ConcurrentBag<(string Host, string Output)> _results = new ConcurrentBag<(string, string)>();

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

            var servers = this.Server.SelectMany(s => ExpandIpRange(s)).ToList();

            // 並列実行オプションの設定
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = this.MaxParallelThreads
            };

            // 並列でSSHコマンドを実行
            Parallel.ForEach(servers, parallelOptions, server =>
            {
                try
                {
                    ConnectionInfo info = new ConnectionInfo(
                        server,
                        this.Port ?? 22,
                        this.User,
                        new PasswordAuthenticationMethod(this.User, this.Password));
                    RunSshCommand(info);
                }
                catch (Exception ex)
                {
                    _errors.Add(new ErrorRecord(
                        ex,
                        "ParallelExecutionError",
                        ErrorCategory.OperationStopped,
                        server));
                }
            });

            // メインスレッドで結果を出力
            foreach (var (host, output) in _results.OrderBy(r => r.Host))
            {
                WriteObject($"[{host}]{Environment.NewLine}{output}");
            }

            // エラーを出力
            foreach (var error in _errors)
            {
                WriteError(error);
            }
        }

        protected override void EndProcessing()
        {
            base.EndProcessing();
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
                    _errors.Add(new ErrorRecord(
                        new Exception("Failed to connect to the SSH server."),
                        "ConnectionFailed",
                        ErrorCategory.ConnectionError,
                        info.Host));
                    return;
                }

                if (this.Sudo)
                {
                    using (var shell = client.CreateShellStream("terminal", 80, 24, 800, 600, 1024))
                    {
                        // 初期プロンプトを待つ（重要）
                        var initialPrompt = shell.Expect(new Regex(@"[\$#>]\s*$"), TimeSpan.FromSeconds(5));
                        if (string.IsNullOrEmpty(initialPrompt))
                        {
                            _errors.Add(new ErrorRecord(
                                new Exception("Failed to get initial shell prompt."),
                                "ShellInitializationFailed",
                                ErrorCategory.ConnectionError,
                                info.Host));
                            return;
                        }

                        var output = new StringBuilder();
                        foreach (var command in this.Command)
                        {
                            shell.WriteLine($"sudo -S {command}");

                            // パスワードプロンプトまたはコマンド実行完了を待つ
                            var response = shell.Expect(
                                new Regex(@"(\[sudo\] password for .+:|password:|[\$#>]\s*$)", RegexOptions.IgnoreCase), 
                                TimeSpan.FromSeconds(this.TimeoutSeconds));
                            
                            if (string.IsNullOrEmpty(response))
                            {
                                _errors.Add(new ErrorRecord(
                                    new Exception($"Command '{command}' timed out."),
                                    "CommandTimeout",
                                    ErrorCategory.OperationTimeout,
                                    info.Host));
                                break;
                            }

                            // パスワードプロンプトが含まれている場合
                            if (response.Contains("password", StringComparison.OrdinalIgnoreCase))
                            {
                                shell.WriteLine(this.Password);
                                // コマンド実行完了を待つ
                                var result = shell.Expect(new Regex(@"[\$#>]\s*$"), TimeSpan.FromSeconds(this.TimeoutSeconds));
                                output.AppendLine(result);
                            }
                            else
                            {
                                // パスワード不要の場合（sudoキャッシュ有効時など）
                                output.AppendLine(response);
                            }
                        }
                        
                        var finalOutput = CleanOutput(output.ToString());
                        SaveOutput(info.Host, finalOutput);
                        
                        // 結果をコレクションに追加（メインスレッドで後で出力）
                        _results.Add((info.Host, finalOutput));
                    }
                }
                else
                {
                    var output = new StringBuilder();
                    foreach (var command in this.Command)
                    {
                        var cmd = client.CreateCommand(command);
                        cmd.CommandTimeout = TimeSpan.FromSeconds(this.TimeoutSeconds);
                        cmd.Execute();

                        output.AppendLine(cmd.Result);
                        if (!string.IsNullOrEmpty(cmd.Error))
                        {
                            output.AppendLine(cmd.Error);
                        }
                    }

                    string outputText = output.ToString();
                    SaveOutput(info.Host, outputText);
                    
                    // 結果をコレクションに追加（メインスレッドで後で出力）
                    _results.Add((info.Host, outputText));
                }

                client.Disconnect();
            }
        }

        private void SaveOutput(string host, string output)
        {
            if (!string.IsNullOrEmpty(this.OutputFile))
            {
                lock (_writeLock)
                {
                    File.AppendAllText(this.OutputFile, $"[{host}]\n{output}\n\n");
                }
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
                    Regex.IsMatch(line, @"^[^@]+@[^:]+:.*[\$#>]\s*$"))
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
