using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using Renci.SshNet;
using System.IO;
using Renci.SshNet.Common;
using System.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.PowerShell.Commands;
using System.Diagnostics;

namespace PSSsh.Cmdlet
{
    [Cmdlet(VerbsDiagnostic.Test, "Process")]
    public class TestProcess : PSCmdlet
    {
        [Parameter]
        public byte[] Key { get; set; }
        [Parameter]
        public SecureString SecureKey { get; set; }


        protected override void ProcessRecord()
        {


        }
    }
}

