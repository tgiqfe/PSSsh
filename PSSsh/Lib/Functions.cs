using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSSsh.Lib
{
    internal class Functions
    {
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
    }
}
