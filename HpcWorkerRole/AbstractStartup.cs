using HSR.AzureEE.Controller.Storage;
using Ionic.Zip;
using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSR.AzureEE.HpcWorkerRole
{
    public abstract class AbstractStartup
    {
        protected List<string> availableJobs = new List<string>();
        protected int instanceIndex { get; set; }
        protected string hpcDeploymentLabel { get; set; }
        protected AzureStorageHelper azureStorage { get; set; }

        public void Configure(string hpcDeploymentLabel, int instanceId, string storageConnection)
        {
            this.hpcDeploymentLabel = hpcDeploymentLabel;
            this.instanceIndex = instanceId;
            this.azureStorage = new AzureStorageHelper(hpcDeploymentLabel, storageConnection);
            
        }

        protected string GetJobDirectory(string jobid)
        {
            string basePath = RoleEnvironment.GetLocalResource("jobdata").RootPath;
            return Path.Combine(basePath, jobid);
        }

        public abstract void Start();
        public abstract void RunJob();

        protected void MakeNextJobReady()
        {
            var nextJob = this.azureStorage.GetNextJob();

            if (nextJob == null)
            {
                return; //do nothing
            }
            if (availableJobs.Contains(nextJob.RowKey))
            {
                return;
            }

            azureStorage.WriteLog("Dequeed Job " + nextJob.RowKey);

            //download job to a new temporary directory
            var newDir = Directory.CreateDirectory(GetJobDirectory(nextJob.RowKey));
            var zipFilePath = Path.Combine(newDir.FullName, "job.zip");
            azureStorage.DownloadBlob(nextJob.FileName, zipFilePath);

            var zipFile = new ZipFile(zipFilePath);
            zipFile.ExtractAll(newDir.FullName, ExtractExistingFileAction.OverwriteSilently);
            availableJobs.Add(nextJob.RowKey);
            azureStorage.WriteLog("Job ready " + nextJob.RowKey);
            SetInstanceActive();
        }

        
        public void SetInstanceActive()
        { 
            //get IP of this instance
            var internalIP = RoleEnvironment.CurrentRoleInstance.InstanceEndpoints["mpiEndpointTCP"].IPEndpoint.Address.ToString();

            //set this host as ready
            azureStorage.SetHostStatus(instanceIndex, internalIP, availableJobs);
            azureStorage.WriteLog("Instance state update");
        }
    }
}
