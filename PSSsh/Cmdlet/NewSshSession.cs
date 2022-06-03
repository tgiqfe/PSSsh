using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using PSSsh.Lib;

namespace PSSsh.Cmdlet
{
    [Cmdlet(VerbsCommon.New, "SshSession")]
    internal class NewSshSession : PSCmdletExtension
    {
    }
}
