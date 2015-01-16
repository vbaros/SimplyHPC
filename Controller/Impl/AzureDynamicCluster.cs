using HSR.AzureEE.Controller.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSR.AzureEE.Controller.Impl
{
    /// <summary>
    /// Abstracts a Cluster that is on Azure 
    /// </summary>
    public class AzureDynamicCluster : IAzureCluster
    {

        IAzureStorageAdapter _storage;
        ICloudController _controller;
        IDeploymentTemplate _template;
        string _name;
        string _deploymentBlobName;
        int _instanceCount;
        string _storageAccountName;
        string _storageAccountKey;
        AzureInstanceType _instanceType;

        
        public AzureDynamicCluster(IAzureStorageAdapter storage, ICloudController controller)
        {
            _storage = storage;
            _controller = controller;
 
        }

        public void Initialize(string configFilePath)
        {
            throw new NotImplementedException();
        }

        public void Initialize(string name, string storageaccountname, string storageaccountkey, AzureInstanceType instanceType, int count, IDeploymentTemplate template)
        {
            _template = template;
            _name = name;
            _storageAccountName = storageaccountname;
            _storageAccountKey = storageaccountkey;
            _deploymentBlobName = name + ".cspkg";
            _instanceType = instanceType;
            _instanceCount = count;
        }

        /// <summary>
        /// Packs and Uploads the Deployment Template
        /// Creates cloud services 
        /// Creates configuration
        /// starts cloud service
        /// </summary>
        public void CreateCluster()
        {
            //set up template
            var csPackPath = Path.GetTempFileName();
            var csCfgPath = Path.GetTempFileName();
            _template.Customize(_name, _storageAccountName, _storageAccountKey, _instanceType, _instanceCount);
            _template.Pack(csPackPath, csCfgPath);

            //write configuration to azure
            _storage.WriteConfiguration(new DeploymentConfiguration()
            {
                InstanceCount = _instanceCount,
                DeploymentLabel = _name
            });

            //upload cspack
            var blobUri = _storage.UploadBlob(this._deploymentBlobName,csPackPath); //TODO: Check name!

            //create cloud service
            _controller.CreateCloudService(this._name);

            //start cloud service with the given uri and config
            _controller.StartCloudService(this._name, blobUri, csCfgPath);
        }

        public ClusterState GetState()
        {
            return new ClusterState()
            {
                InstanceStates = _controller.GetCloudServiceState(this._name)
            };

        }

        /// <summary>
        /// Uploads and submits a job
        /// </summary>
        /// <param name="job">Meta data information about the job</param>
        /// <param name="jobDataPath">Local path to the zip file containing the job</param>
        /// <returns></returns>
        public string SubmitJob(JobItem job, string jobDataPath)
        {
            _storage.UploadBlob(Path.GetFileName(jobDataPath),jobDataPath);
            job.FileName = Path.GetFileName(jobDataPath);
            _storage.WriteJob(job);
            return job.RowKey;
        }

        public bool DownloadJobResults(string jobId)
        {
            if (HasJobFinished(jobId))
            {
                var job = _storage.GetJobs().Where(x => x.RowKey == jobId).First();

                if (job.ResultFileName.Length > 0)
                {
                    _storage.DownloadBlob(job.ResultFileName, job.ResultFileName);
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }

        public bool HasJobFinished(string jobId)
        {
            var job = _storage.GetJobs().Where(x => x.RowKey == jobId);

            if (job.Any())
            {
                return job.First().Finished;
            }
            return false;
        }

        public List<JobItem> GetJobs()
        {
            return _storage.GetJobs();
        }

        public void DeleteCluster()
        {
            _controller.DeleteCloudService(this._name);
        }
    }
}
