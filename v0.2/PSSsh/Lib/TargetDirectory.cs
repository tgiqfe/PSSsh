using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSSsh.Lib
{
    internal class TargetDirectory
    {
        /// 対象ファイルの親フォルダーを作成
        /// </summary>
        /// <param name="targetPath"></param>
        public static void CreateParent(string targetPath)
        {
            if (targetPath.Contains(Path.DirectorySeparatorChar))
            {
                string parent = Path.GetDirectoryName(targetPath);
                if (!Directory.Exists(parent))
                {
                    Directory.CreateDirectory(parent);
                }
            }
        }
    }
}
