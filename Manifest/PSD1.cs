using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Manifest
{
    //  v0.01.006
    class PSD1
    {
        public static void Create(ProjectInfo info)
        {
            if (!File.Exists(info.DllFile)) { return; }

            string dllFile_absolute = Path.GetFullPath(info.DllFile);

            //  Cmdletを探してセット
            List<string> CmdletsToExportList = new List<string>();
            foreach (string csFile in Directory.GetFiles(info.CmdletDir, "*.cs", SearchOption.AllDirectories))
            {
                if (info.ExcludeCmdlet.Any(x => x.Equals(Path.GetFileName(csFile), StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
                using (StreamReader sr = new StreamReader(csFile, Encoding.UTF8))
                {
                    string readLine = "";
                    while ((readLine = sr.ReadLine()) != null)
                    {
                        if (Regex.IsMatch(readLine, @"^\s*\[Cmdlet\(Verbs"))
                        {
                            string cmdPre = readLine.Substring(
                                readLine.IndexOf(".") + 1, readLine.IndexOf(",") - readLine.IndexOf(".") - 1);
                            string cmdSuf = readLine.Substring(
                                readLine.IndexOf("\"") + 1, readLine.LastIndexOf("\"") - readLine.IndexOf("\"") - 1);
                            CmdletsToExportList.Add(cmdPre + "-" + cmdSuf);
                        }
                    }
                }
            }

            //  Format.ps1xmlを探してセット
            List<string> FormatsToProcessList = new List<string>();
            if (Directory.Exists(info.FormatDir))
            {
                foreach (string formatFile in Directory.GetFiles(info.FormatDir, "*.ps1xml"))
                {
                    FormatsToProcessList.Add(Path.GetFileName(formatFile));
                }
            }

            //  バージョン取得
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(info.DllFile);

            //  GUID取得
            GuidAttribute attr =
                Attribute.GetCustomAttribute(Assembly.LoadFile(dllFile_absolute), typeof(GuidAttribute)) as GuidAttribute;

            string RootModule = Path.GetFileName(info.DllFile);
            string ModuleVersion = fvi.FileVersion;
            string Guid = attr.Value;
            string Author = info.Author;
            string CompanyName = info.CompanyName;
            string Copyright = string.IsNullOrEmpty(info.Copyright) ? fvi.LegalCopyright : info.Copyright;
            string Description = info.Description;

            string manifestString = string.Format(@"@{{
RootModule = ""{0}""
ModuleVersion = ""{1}""
GUID = ""{2}""
Author = ""{3}""
CompanyName = ""{4}""
Copyright = ""{5}""
Description = ""{6}""
CmdletsToExport = @(
  ""{7}""
)
FormatsToProcess = @({8})
}}",
RootModule, ModuleVersion, Guid, Author, CompanyName, Copyright, Description,
string.Join("\",\r\n  \"", CmdletsToExportList),
FormatsToProcessList.Count > 0 ? "\r\n  \"" + string.Join("\",\r\n  \"", FormatsToProcessList) + "\"\r\n" : ""
);
            using (StreamWriter sw = new StreamWriter(info.Psd1File, false, Encoding.UTF8))
            {
                sw.WriteLine(manifestString);
            }
        }
    }
}
