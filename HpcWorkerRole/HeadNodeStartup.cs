using HSR.AzureEE.Controller.Storage;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HSR.AzureEE.MpiWrapper;

namespace HSR.AzureEE.HpcWorkerRole
{
    public class HeadNodeStartup : AbstractStartup
    {
        List<HostInformation> hosts;
   
        public override void Start()
        {
            SetInstanceActive();

            //get full configuration
            var deploymentConfig = this.azureStorage.GetConfiguration();

            //wait for all nodes to appear
            hosts = this.azureStorage.GetHosts();

            while (hosts.Count != deploymentConfig.InstanceCount)
            {
                azureStorage.WriteLog("Not all hosts online yet - waiting");
                Thread.Sleep(5000);
                hosts = this.azureStorage.GetHosts();
            }

            azureStorage.WriteLog("Hosts " + hosts.Select(x => x.Address).Aggregate((x, y) => x + "," + y) + " online");
                    
        }



        public override void RunJob()
        {
            //prepare new jobs
            base.MakeNextJobReady();



            var nextJob = this.azureStorage.GetNextJob();

            if (nextJob == null)
            {
                return;
            }
       
            //update host information
            hosts = this.azureStorage.GetHosts();

            if (!hosts.All(x => x.ReadyJobs.Split(',').Contains(nextJob.RowKey)))
            {
                azureStorage.WriteLog("Job " + nextJob.RowKey + " not ready on all nodes");
                return;
            }

            //run the job!
            var executable = Path.Combine(GetJobDirectory(nextJob.RowKey), nextJob.Executable);

            azureStorage.WriteLog("Starting " + nextJob.RowKey +" with exe ="+executable+", params="+nextJob.Parameters ?? "");

            string blobName = null;
            try
            {
                //connect to runner
                ChannelFactory<IMPIRunner> pipeFactory = new ChannelFactory<IMPIRunner>(
                    new NetNamedPipeBinding(),
                    new EndpointAddress("net.pipe://localhost/MPIRunner")
                    );

                //connect to the MPI Wrapper processs using Interface IMPIRunner
                IMPIRunner runner = pipeFactory.CreateChannel();

                //run the exe using the Wrapper
                switch (nextJob.JobType)
                {
                    case (int)JobItem.Type.MPI:
                        runner.RunApplication(executable, nextJob.Parameters, hosts.Select(x => x.Address).ToArray(), nextJob.CorePerNode, nextJob.NumNodes);
                        break;
                    case (int)JobItem.Type.Normal:
                        runner.RunApplication(executable, nextJob.Parameters);
                        break;
                }
                

                //Wait for the exe to terminate
                while (runner.GetState() == RunnerState.Running)
                {
                    azureStorage.WriteLog("Job " + nextJob.RowKey + " running...");

                    //var lastOutput = runner.GetCurrentStandardOutput();
                    //azureStorage.WriteLog(lastOutput);

                    Thread.Sleep(10000);
                }

                //retrieve results and upload to blob storage
                azureStorage.WriteLog("Uploading job results");
                blobName = nextJob.RowKey + "-" + DateTime.Now.ToString("HHMMss") + "-" + nextJob.InfoTag + "-" + nextJob.NumNodes + "n-" + nextJob.CorePerNode + "c-result.zip";
                azureStorage.UploadBlob(blobName, runner.GetResultFilePath());
                

                azureStorage.WriteLog("Job " + nextJob.RowKey + " finished");
                pipeFactory.Close();
            }
            catch (Exception ex)
            {

                azureStorage.WriteLog("Job " + nextJob.RowKey + " failed ");
                azureStorage.WriteLog(ex.ToString());
            }
                      
            
            //set job to  finished
            this.azureStorage.SetJobState(nextJob.RowKey, true, blobName != null ? blobName.ToString() : string.Empty);

            
        }
    }
}
