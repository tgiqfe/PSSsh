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
    public class NewSshSession : PSCmdletExtension
    {
        #region Command Parameter

        [Parameter(Position = 0)]
        public string Server { get; set; }

        [Parameter]
        public int? Port { get; set; }

        [Parameter(Position = 1)]
        public string User { get; set; }

        [Parameter(Position = 2)]
        public string Password { get; set; }

        [Parameter]
        public string PasswordFile { get; set; }

        [Parameter]
        public PSCredential Credential { get; set; }

        [Parameter]
        public SwitchParameter KeyboardInteractive { get; set; }

        #endregion

        protected override void BeginProcessing()
        {
            base.BeginProcessing();

            this.User = GetUserName(this.User, this.Credential);
            this.Password = GetPassword(this.Password, this.Credential, this.PasswordFile);
        }

        protected override void ProcessRecord()
        {
            var session = new SshSession()
            {
                Server = this.Server,
                Port = this.Port,
                User = this.User,
                Password = this.Password,
                KeyboardInteractive = this.KeyboardInteractive,
            };

            WriteObject(session, true);
        }
    }
}
