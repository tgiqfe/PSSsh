using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PSSsh.Lib;

namespace PSSsh.Logs
{
    internal class LoggerBase : IDisposable
    {
        /// <summary>
        /// ログ記述用ロック。静的パラメータ
        /// </summary>
        private static AsyncLock _lock = null;

        private string _logFilePath = null;
        private StreamWriter _writer = null;

        protected virtual bool _logAppend { get; }
        protected virtual string _tag { get; }

        public void Init(string logDir, string logPreName)
        {
            _lock ??= new AsyncLock();

            string today = DateTime.Now.ToString("yyyyMMdd");
            _logFilePath = Path.Combine(logDir, $"{logPreName}_{today}.log");
            TargetDirectory.CreateParent(_logFilePath);
            _writer = new StreamWriter(_logFilePath, _logAppend, new UTF8Encoding(false));
        }

        #region Close

        ~LoggerBase()
        {
            Close();
        }

        public virtual async Task CloseAsync()
        {
            using (await _lock.LockAsync())
            {
                if (_writer != null) _writer.Dispose(); _writer = null;
            }
        }

        public virtual void Close()
        {
            CloseAsync().Wait();
        }

        #endregion
        #region Dispose

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Close();
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
