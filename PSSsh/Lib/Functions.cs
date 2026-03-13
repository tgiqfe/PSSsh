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
        /// ログファイル出力
        /// </summary>
        private static readonly object _writeLock = new object();
        public static void SaveOutput(string outputFile, string outputDirectory, string host, string output)
        {
            if (!string.IsNullOrEmpty(outputFile))
            {
                lock (_writeLock)
                {
                    File.AppendAllText(outputFile, $"[{host}]\n{output}\n\n");
                }
            }
            if (!string.IsNullOrEmpty(outputDirectory))
            {
                var fileName = $"{host}_{DateTime.Now:yyyyMMddHHmmss}.txt";
                var filePath = Path.Combine(outputDirectory, fileName);
                File.WriteAllText(filePath, output);
            }
        }
    }
}
