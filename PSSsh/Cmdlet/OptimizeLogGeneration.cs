using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using System.IO;

namespace PSSsh.Cmdlet
{
    /// <summary>
    /// 対象フォルダーから、指定世代以上古いファイル/フォルダー、指定日時より古いファイル/フォルダーを削除
    /// ログローテーションを目的に使用することを推奨。
    /// </summary>
    [Cmdlet(VerbsCommon.Optimize, "LogGeneration")]
    public class OptimizeLogGeneration : PSCmdlet
    {
        const string PATHTYPE_FILE = "File";
        const string PATHTYPE_DIRECTORY = "Directory";
        const string BASE_NAME = "Name";
        const string BASE_DATE = "Date";

        [Parameter(Mandatory = true, Position = 0), Alias("Path")]
        public string TargetDirectory { get; set; }
        [Parameter, ValidateSet(PATHTYPE_FILE, PATHTYPE_DIRECTORY)]
        public string PathType { get; set; } = PATHTYPE_FILE;
        [Parameter, ValidateSet(BASE_NAME, BASE_DATE)]
        public string Base { get; set; } = BASE_NAME;
        [Parameter]
        [LogNotNull]
        public int RetentionCount { get; set; }
        [Parameter]
        [LogNotNull]
        public int RetentionDays { get; set; }
        [Parameter]
        [LogNotNull]
        public DateTime DeadLine { get; set; }
        [Parameter]
        [LogIgnore]
        public SwitchParameter Dryrun { get; set; }
        [Parameter]
        [LogNotNull]
        public SwitchParameter DebugMode { get; set; }

        protected override void BeginProcessing()
        {
            //  カレントディレクトリカレントディレクトリの一時変更
            Item.CurrentDirectory = Environment.CurrentDirectory;
            Environment.CurrentDirectory = this.SessionState.Path.CurrentFileSystemLocation.Path;

            if (RetentionCount < 0) { RetentionCount = 0; }

            bool debugMode = DebugMode;
#if DEBUG
            debugMode = true;
#endif
            Item.Logger = Function.SetLogger(Item.LOG_DIRECTORY, "OptimizeLogGeneration", debugMode);
            Item.Logger.Info(Function.GetPropertyString<OptimizeLogGeneration>(this));

        }

        protected override void ProcessRecord()
        {
            if (Directory.Exists(TargetDirectory))
            {
                string[] items = PathType == PATHTYPE_FILE ?
                    Directory.GetFiles(TargetDirectory) :
                    Directory.GetDirectories(TargetDirectory);

                string[] removeItems = null;
                if (Base == BASE_NAME)
                {
                    removeItems = items.OrderBy(x => string.Format(Path.GetFileName(x))).
                        Reverse().
                        Where((name, index) => index >= RetentionCount).
                        ToArray();
                }
                else if (Base == BASE_DATE)
                {
                    if (RetentionCount > 0)
                    {
                        removeItems = items.OrderBy(x => File.GetLastWriteTime(x)).
                            Reverse().
                            Where((name, index) => index >= RetentionCount).
                            ToArray();
                    }
                    else if (RetentionDays > 0)
                    {
                        DateTime tempDeadLine = DateTime.Today.AddDays(RetentionDays * -1);
                        removeItems = items.OrderBy(x => File.GetLastWriteTime(x)).
                            Reverse().
                            Where(x => File.GetLastWriteTime(x) < tempDeadLine).
                            ToArray();
                    }
                    else
                    {
                        removeItems = items.Where(name => File.GetLastWriteTime(name) < DeadLine).ToArray();
                    }
                }

                foreach (string removeItem in removeItems)
                {
                    if (File.Exists(removeItem))
                    {
                        if (!Dryrun)
                        {
                            File.Delete(removeItem);
                        }
                        Item.Logger.Info("DeleteFile: {0}", Path.GetFileName(removeItem));
                    }
                    else if (Directory.Exists(removeItem))
                    {
                        if (!Dryrun)
                        {
                            Directory.Delete(removeItem, true);
                        }
                        Item.Logger.Info("DeleteDirectory: {0}", Path.GetFileName(removeItem));
                    }
                }
            }
        }

        protected override void EndProcessing()
        {
            //  カレントディレクトリを戻す
            Environment.CurrentDirectory = Item.CurrentDirectory;

            //  Loggerを終了
            Item.Logger = null;
        }
    }
}
