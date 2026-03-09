using Renci.SshNet.Compression;
using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;

namespace PSSsh.Cmdlet
{
    public class PSCmdletExtension : PSCmdlet
    {
        #region Change CurrentDirectory

        private string _currentDirectory = null;

        protected override void BeginProcessing()
        {
            //  Change the current directory temporarily to the current file system location of the session state.
            _currentDirectory = Environment.CurrentDirectory;
            Environment.CurrentDirectory = SessionState.Path.CurrentFileSystemLocation.Path;
        }

        protected override void EndProcessing()
        {
            //  Restore the current directory
            Environment.CurrentDirectory = _currentDirectory;
        }

        #endregion
    }
}
