using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSR.AzureEE.HpcWorkerRole
{
    public class ComputeNodeStartup : AbstractStartup
    {
    
        public override void Start()
        {
            SetInstanceActive();
        }

        public override void RunJob()
        {
            MakeNextJobReady();
        }
    }
}
