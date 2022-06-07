using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSSsh.Logs
{
    internal class Logger : LoggerBase<LogBody>
    {
        protected override bool _logAppend { get { return true; } }
        protected override string _tag { get { return LogBody.TAG; } }

        public Logger()
        {
            base.Init(Path.Combine(Item.WorkDirectoryPath, "Logs"), "PSSsh");
            Write("開始");
        }

        #region Write

        public async Task WriteAsync(LogLevel level, string title, string message)
        {
            await SendAsync(new LogBody(init: true)
            {
                Date = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
                Level = level,
                Title = title ?? "-",
                Message = message,
            });
        }

        public async Task WriteAsync(LogLevel level, string title, string format, object[] args)
        {
            await WriteAsync(level, title, string.Format(format, args));
        }

        public async Task WriteAsync(string message)
        {
            await WriteAsync(LogLevel.Info, "-", message);
        }

        public void Write(LogLevel level, string title, string message)
        {
            WriteAsync(level, title, message).ConfigureAwait(false);
        }

        public void Wrtie(LogLevel level, string title, string format, object[] args)
        {
            WriteAsync(level, title, string.Format(format, args)).ConfigureAwait(false);
        }

        public void Write(string message)
        {
            WriteAsync(LogLevel.Info, "-", message).ConfigureAwait(false);
        }

        #endregion

        public override async Task CloseAsync()
        {
            Write("終了");

            await base.CloseAsync();
        }
    }
}
