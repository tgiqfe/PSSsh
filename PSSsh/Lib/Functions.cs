using System.Collections.Concurrent;

namespace PSSsh.Lib
{
    internal class Functions
    {
        /// <summary>
        /// IPアドレスの範囲を決定して返す
        /// </summary>
        /// <param name="ipRange"></param>
        /// <returns></returns>
        public static string[] ExpandIpRange(string ipRange)
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


        /// <summary>
        /// 出力結果を保存。
        /// </summary>
        private static readonly object _writeLock = new object();
        public static void SaveOutput(string outputFile, string outputDirectory, string host, string output)
        {
            if (!string.IsNullOrEmpty(outputFile))
            {
                lock (_writeLock)
                {
                    var parent = Path.GetDirectoryName(outputFile);
                    if (!Directory.Exists(parent))
                    {
                        Directory.CreateDirectory(parent);
                    }
                    File.AppendAllText(outputFile, $"[{host}]\n{output}\n\n");
                }
            }
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                if (!Directory.Exists(outputDirectory))
                {
                    Directory.CreateDirectory(outputDirectory);
                }
                var fileName = $"{host}_{DateTime.Now:yyyyMMddHHmmss}.txt";
                var filePath = Path.Combine(outputDirectory, fileName);
                File.WriteAllText(filePath, output);
            }
        }

        //  出力結果を改めて保存。
        public static void SaveFinalOutput(ConcurrentBag<(string Host, string Output)> results, string outputFile)
        {
            lock (_writeLock)
            {
                var parent = Path.GetDirectoryName(outputFile);
                if (!Directory.Exists(parent))
                {
                    Directory.CreateDirectory(parent);
                }
                using (var writer = new StreamWriter(outputFile, false))
                {
                    bool isFirst = true;
                    foreach (var (host, output) in results.OrderBy(r => r.Host))
                    {
                        if (!isFirst)
                        {
                            writer.WriteLine(string.Empty);
                        }
                        writer.WriteLine($"[{host}]{Environment.NewLine}{output}");
                        isFirst = false;
                    }

                }
            }
        }
    }
}
