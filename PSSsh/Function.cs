using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.Reflection;

namespace PSSsh
{
    internal class Function
    {
        /// <summary>
        /// ログ出力設定
        /// </summary>
        /// <param name="preName"></param>
        /// <returns></returns>
        public static Logger SetLogger(string logDir, string preName, bool debugMode)
        {
            if (!Directory.Exists(logDir)) { Directory.CreateDirectory(logDir); }

            string logPath = System.IO.Path.Combine(
                logDir,
                string.Format("{0}_{1}.log", preName, DateTime.Now.ToString("yyyyMMdd")));

            //  ファイル出力先設定
            FileTarget file = new FileTarget("File");
            file.Encoding = Encoding.GetEncoding("Shift_JIS");
            file.Layout = "[${longdate}][${windows-identity}][${uppercase:${level}}] ${message}";
            //file.Layout = "[${longdate}][${uppercase:${level}}] ${message}";
            file.FileName = logPath;

            //  コンソール出力設定
            ConsoleTarget console = new ConsoleTarget("Console");
            //console.Layout = "[${longdate}][${windows-identity}][${uppercase:${level}}] ${message}";
            console.Layout = "[${longdate}][${uppercase:${level}}] ${message}";

            LoggingConfiguration conf = new LoggingConfiguration();
            conf.AddTarget(file);
            conf.AddTarget(console);
            if (debugMode)
            {
                conf.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, file));
                conf.LoggingRules.Add(new LoggingRule("*", LogLevel.Debug, console));
            }
            else
            {
                conf.LoggingRules.Add(new LoggingRule("*", LogLevel.Info, file));
            }
            LogManager.Configuration = conf;
            Logger logger = LogManager.GetCurrentClassLogger();

            return logger;
        }

        /// <summary>
        /// 対象ファイルの親フォルダーの有無を確認。無ければフォルダー作成
        /// </summary>
        /// <param name="targetFile"></param>
        public static void CheckParentDirectory(string targetFile)
        {
            if (targetFile.Contains("\\"))
            {
                if (!Directory.Exists(Path.GetDirectoryName(targetFile)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(targetFile));
                }
            }
        }

        /// <summary>
        /// ログ用プロパティ情報の文字列を取得
        /// </summary>
        /// <returns></returns>
        public static string GetPropertyString<T>(T obj)
        {
            StringBuilder sb = new StringBuilder();
            List<string> propertyNameList = new List<string>();
            List<string> propertyValueList = new List<string>();
            foreach (PropertyInfo pi in
                typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                //  LogIgnore属性があるプロパティは表記しない
                LogIgnoreAttribute logIgnoreAttr = Attribute.GetCustomAttribute(pi, typeof(LogIgnoreAttribute)) as LogIgnoreAttribute;
                //  値がnullでLogNotNull属性がある場合は表記しない
                LogNotNullAttribute logNotNullAttribute = Attribute.GetCustomAttribute(pi, typeof(LogNotNullAttribute)) as LogNotNullAttribute;

                if (logIgnoreAttr == null)
                {
                    object paramValue = pi.GetValue(obj);
                    if (paramValue == null && logNotNullAttribute == null)
                    {
                        propertyNameList.Add(pi.Name);
                        propertyValueList.Add("-");
                    }
                    else
                    {
                        switch (paramValue)
                        {
                            case string str:
                                if (logNotNullAttribute == null || !string.IsNullOrEmpty(str))
                                {
                                    propertyNameList.Add(pi.Name);
                                    propertyValueList.Add(str);
                                }
                                break;
                            case int int32:
                                if (logNotNullAttribute == null || int32 != 0)
                                {
                                    propertyNameList.Add(pi.Name);
                                    propertyValueList.Add(int32.ToString());
                                }
                                break;
                            case long int64:
                                if (logNotNullAttribute == null || int64 != 0)
                                {
                                    propertyNameList.Add(pi.Name);
                                    propertyValueList.Add(int64.ToString());
                                }
                                break;
                            case string[] array:
                                if(logNotNullAttribute == null || array.Length > 0)
                                {
                                    propertyNameList.Add(pi.Name);
                                    propertyValueList.Add(string.Join(", ", array));
                                }
                                break;
                            case DateTime datetime:
                                if (logNotNullAttribute == null || datetime > DateTime.Parse("0001/01/01 0:00:00"))
                                {
                                    propertyNameList.Add(pi.Name);
                                    propertyValueList.Add(datetime.ToString("yyyy/MM/dd HH:mm:ss"));
                                }
                                break;
                            case bool bol:
                                if (logNotNullAttribute == null || bol)
                                {
                                    propertyNameList.Add(pi.Name);
                                    propertyValueList.Add(bol.ToString());
                                }
                                break;
                        }
                    }
                }
            }
            int maxLength = propertyNameList.Max(x => x.Length);

            for (int i = 0; i < propertyNameList.Count; i++)
            {
                sb.Append(string.Format("\r\n  {0}: {1}",
                    propertyNameList[i].PadRight(maxLength, ' '), propertyValueList[i]));
            }
            return sb.ToString();
        }
    }
}
