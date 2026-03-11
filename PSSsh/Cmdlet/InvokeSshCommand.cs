using PSSsh.Lib;
using Renci.SshNet;
using System.Collections.Concurrent;
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
        public int MaxParallelThreads { get; set; }

        [Parameter]
        public int TimeoutSeconds { get; set; } = 7200;

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

            var servers = this.Server.SelectMany(s => Functions.ExpandIpRange(s)).ToList();

            if (this.MaxParallelThreads <= 0)
            {
                //  並列実行オプションが無効な場合は、順次実行
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
            else
            {
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
            }

            // メインスレッドで結果を出力
            bool isFirst = true;
            foreach (var (host, output) in _results.OrderBy(r => r.Host))
            {
                if (!isFirst)
                {
                    WriteObject(string.Empty);
                }
                WriteObject($"[{host}]{Environment.NewLine}{output}");
                isFirst = false;
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
                    //  Sudoモード
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
                                    if (Regex.IsMatch(data, @"[%\$#>]\s*", RegexOptions.Multiline))
                                    {
                                        break;
                                    }
                                }
                                else if ((DateTime.Now - commandStartTime) > maxTimeout)
                                {
                                    // タイムアウト
                                    _errors.Add(new ErrorRecord(
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

                        var outputText = CleanOutput(output.ToString());
                        Functions.SaveOutput(this.OutputFile, this.OutputDirectory, info.Host, outputText);

                        // 結果をコレクションに追加（メインスレッドで後で出力）
                        _results.Add((info.Host, outputText));
                    }
                }
                else
                {
                    //  通常モード
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
                    Functions.SaveOutput(this.OutputFile, this.OutputDirectory, info.Host, outputText);

                    // 結果をコレクションに追加（メインスレッドで後で出力）
                    _results.Add((info.Host, outputText));
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
