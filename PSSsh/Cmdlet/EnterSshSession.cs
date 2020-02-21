using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using Renci.SshNet;
using System.Threading;

namespace PSSsh.Cmdlet
{
    //  検討中
    /*
    private void ExecuteSSH(SshClient ssh, string[] cmdList)
    {
        using (ShellStream ss = ssh.CreateShellStream("ssh", 180, 324, 1800, 3600, 8000))
        {
            foreach (string command in cmdList)
            {
                ss.Write(command + "\n");
                Thread.Sleep(500);
                string tempStr = ss.Read();
                if (!string.IsNullOrEmpty(tempStr))
                {
                    Console.WriteLine(tempStr);
                }
            }
        }
    }
    */

    //  検討中
    //  RunAsyncオプション時に使用できそうな処理なのでメモ
    /*
    private async Task ExecuteAsync(SshCommand cmd)
    {
        var asyncResult = cmd.BeginExecute();

        StreamReader stdout = new StreamReader(cmd.OutputStream);
        StreamReader stderr = new StreamReader(cmd.ExtendedOutputStream);
        while (!asyncResult.IsCompleted)
        {
            await Task.Run(async () =>
            {
                string outLine = await stdout.ReadToEndAsync();
                if (!string.IsNullOrEmpty(outLine)) { Console.WriteLine(outLine); }
            });
            await Task.Run(async () =>
            {
                string errLine = await stderr.ReadToEndAsync();
                if (!string.IsNullOrEmpty(errLine)) { Console.WriteLine(errLine); }
            });
        }

        cmd.EndExecute(asyncResult);
    }
    */
}


