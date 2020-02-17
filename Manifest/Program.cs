using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.IO.Compression;

namespace Manifest
{
    //  v0.01.006
    class Program
    {
        static void Main(string[] args)
        {
            Environment.CurrentDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            CreateDebugModule(new ProjectInfo() { Mode = Mode.Debug });
            CreateReleaseModule(new ProjectInfo() { Mode = Mode.Release });
        }

        //  Debugモジュール作成
        private static void CreateDebugModule(ProjectInfo info)
        {
            PSD1.Create(info);
            PSM1.Create(info);
        }

        //  Releaseモジュール作成
        private static void CreateReleaseModule(ProjectInfo info)
        {
            PSD1.Create(info);
            PSM1.Create(info);

            //  Releaseフォルダーを公開用にコピー
            if (Directory.Exists(info.TargetDir))
            {
                using (Process proc = new Process())
                {
                    proc.StartInfo.FileName = "robocopy.exe";
                    proc.StartInfo.Arguments = string.Format(
                        "\"{0}\" \"{1}\" /COPY:DAT /MIR /E /XJD /XJF", info.TargetDir, info.ModuleDir);
                    proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    proc.Start();
                    proc.WaitForExit();
                }
            }

            //  Scriptフォルダーをコピー
            if (Directory.Exists(info.ScriptDir))
            {
                foreach (string fileName in Directory.GetFiles(info.ScriptDir))
                {
                    File.Copy(fileName, Path.Combine(info.ModuleDir, Path.GetFileName(fileName)), true);
                }
                using (Process proc = new Process())
                {
                    proc.StartInfo.FileName = "robocopy.exe";
                    proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                    foreach (string dirName in Directory.GetDirectories(info.ScriptDir))
                    {
                        proc.StartInfo.Arguments = string.Format(
                            "\"{0}\" \"{1}\" /COPY:DAT /MIR /E /XJD /XJF",
                            dirName,
                            Path.Combine(info.ModuleDir, Path.GetFileName(dirName)));
                        proc.Start();
                        proc.WaitForExit();
                    }
                }
            }

            //  フォーマットファイルをコピー
            if (Directory.Exists(info.FormatDir))
            {
                foreach (string fileName in Directory.GetFiles(info.FormatDir, "*.ps1xml"))
                {
                    File.Copy(fileName, Path.Combine(info.ModuleDir, Path.GetFileName(fileName)), true);
                }
                using (Process proc = new Process())
                {
                    proc.StartInfo.FileName = "robocopy.exe";
                    proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                    foreach (string dirName in Directory.GetDirectories(info.FormatDir))
                    {
                        proc.StartInfo.Arguments = string.Format(
                            "\"{0}\" \"{1}\" /COPY:DAT /MIR /E /XJD /XJF",
                            dirName,
                            Path.Combine(info.ModuleDir, Path.GetFileName(dirName)));
                        proc.Start();
                        proc.WaitForExit();
                    }
                }
            }

            //  ヘルプファイルをコピー
            if (Directory.Exists(info.HelpDir))
            {
                foreach (string fileName in Directory.GetFiles(info.HelpDir, "*.dll-Help.xml"))
                {
                    File.Copy(fileName, Path.Combine(info.ModuleDir, Path.GetFileName(fileName)), true);
                }
                using (Process proc = new Process())
                {
                    proc.StartInfo.FileName = "robocopy.exe";
                    proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

                    foreach (string dirName in Directory.GetDirectories(info.HelpDir))
                    {
                        proc.StartInfo.Arguments = string.Format(
                            "\"{0}\" \"{1}\" /COPY:DAT /MIR /E /XJD /XJF",
                            dirName,
                            Path.Combine(info.ModuleDir, Path.GetFileName(dirName)));
                        proc.Start();
                        proc.WaitForExit();
                    }
                }
            }

            //  モジュールフォルダーをZipアーカイブ
            if (Directory.Exists(info.ModuleDir))
            {
                if (File.Exists(info.ModuleZip))
                {
                    File.Delete(info.ModuleZip);
                }
                ZipFile.CreateFromDirectory(info.ModuleDir, info.ModuleZip, CompressionLevel.Optimal, false);
            }
        }
    }
}
