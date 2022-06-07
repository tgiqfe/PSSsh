using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSSsh.Logs
{
    internal class LogBody : LogBodyBase
    {
        public const string TAG = "PSSshLog";

        #region Public parameter

        public override string Tag { get { return TAG; } }
        public override string Date { get; set; }
        public override string ProcessName { get; set; }
        public override string HostName { get; set; }
        public override string UserName { get; set; }
        public override LogLevel Level { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }

        #endregion

        public LogBody() { }
        public LogBody(bool init)
        {
            if (init) Init();
        }

        public void Init()
        {
            this.ProcessName = Item.ProcessName;
            this.HostName = Environment.MachineName;
            this.UserName = Environment.UserName;
        }
    }
}
