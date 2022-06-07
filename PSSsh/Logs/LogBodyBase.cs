using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSSsh.Logs
{
    internal class LogBodyBase
    {
        public virtual string Tag { get { return ""; } }
        public virtual string Date { get; set; }
        public virtual string ProcessName { get; set; }
        public virtual string HostName { get; set; }
        public virtual string UserName { get; set; }
        public virtual LogLevel Level { get; set; }
        
        public virtual string GetJson() { return ""; }

        public virtual string ToConsoleMessage() { return ""; }
    }
}
