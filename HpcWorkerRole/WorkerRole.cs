using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using System.IO;

namespace HSR.AzureEE.HpcWorkerRole
{
    public class WorkerRole : RoleEntryPoint
    {
        ManualResetEvent stopEvent = new ManualResetEvent(false);
        AbstractStartup roleStartupMethod;
        //const string ConnectionString = "DefaultEndpointsProtocol=https;AccountName=computeresults;AccountKey=MNYkjC2wSFrayr9OE5hZpY2Ot3N5gJJJCC7+AGL/gu5zBEe6qT8HQimNcNHnjnfvZrtGRbaRg9aQIKJD0KWn3Q==;";
        //const string ConnectionString = "DefaultEndpointsProtocol=https;AccountName=smallstorage1;AccountKey=7HrZERRD0V4UuT2F6mI77Xm2Kb1ql21wfMgrKev97OtZGAatvp/iOE44xesElsoXhvzLp3s7j5hzyp4TNxzNZQ==;";

        public override void Run()
        {
            
            while (!stopEvent.WaitOne(15000))
            {   
                roleStartupMethod.RunJob();
            }
        }

       
        public override bool OnStart()
        {
            //this is the head node!
            if (InstanceIdHelper.CurrentInstanceIndex == 0)
            {
                roleStartupMethod = new HeadNodeStartup();
            }
            else
            {
                roleStartupMethod = new ComputeNodeStartup();
            }

            string ConnectionString = string.Format("DefaultEndpointsProtocol=https;AccountName={0};AccountKey={1};", InstanceIdHelper.CurrentDeploymentStorageAccountName, InstanceIdHelper.CurrentDeploymentStorageAccountKey);

            roleStartupMethod.Configure(InstanceIdHelper.CurrentDeploymentLabel,
                                        InstanceIdHelper.CurrentInstanceIndex,
                                        ConnectionString);

            roleStartupMethod.Start();
                             
            return base.OnStart();
        }

        public override void OnStop()
        {
            stopEvent.Set();
            base.OnStop();
        }
    }
}
