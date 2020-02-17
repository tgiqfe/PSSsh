using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Manifest
{
    //  v0.01.005
    class PSM1
    {
        public static void Create(ProjectInfo info)
        {
            if (!File.Exists(info.DllFile)) { return; }
            using (StreamWriter sw = new StreamWriter(info.Psm1File, false, Encoding.UTF8))
            {
                sw.WriteLine();
            }
        }
    }
}
