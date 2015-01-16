using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;    

namespace HSR.AzureEE.Controller.Storage
{
    public class AzureStorageHelper : IAzureStorageAdapter
    {
        const string ConfigSuffix = "config";
        const string LogSuffix = "log";
        const string MainConfigRowKey = "config";
        const string HostStatusRowKey = "host-";
        const string JobRowKey = "job-";

        private string _deploymentLabel;

        private CloudStorageAccount storageAccount;
        private CloudTableClient tableClient;
        private CloudBlobClient blobClient;

        public AzureStorageHelper(AzureSubscription subscription,string deploymentLabel)
        {
            _deploymentLabel = deploymentLabel;

            storageAccount = new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(subscription.StorageAccount, subscription.StorageAccountKey),true);
            tableClient = storageAccount.CreateCloudTableClient();
            blobClient = storageAccount.CreateCloudBlobClient();
        }

        public AzureStorageHelper(string deploymentLabel, string connectionString)
        {
           _deploymentLabel = deploymentLabel;
           storageAccount = CloudStorageAccount.Parse(connectionString);
           
            tableClient = storageAccount.CreateCloudTableClient();
            blobClient = storageAccount.CreateCloudBlobClient();
        }

        public DeploymentConfiguration GetConfiguration()
        {
            CloudTable configTable = tableClient.GetTableReference(string.Format("{0}{1}", _deploymentLabel, ConfigSuffix));
            var config = from entity in configTable.CreateQuery<DeploymentConfiguration>()
                            where entity.RowKey == MainConfigRowKey
                            select entity;

            return config.Single();
        }

        public void WriteConfiguration(DeploymentConfiguration config)
        {
            //try
            //{
                var configTable = tableClient.GetTableReference(string.Format("{0}{1}", _deploymentLabel, ConfigSuffix));
                configTable.CreateIfNotExists();
                config.PartitionKey = string.Empty;
                config.RowKey = MainConfigRowKey;

                configTable.Execute(TableOperation.InsertOrReplace(config));

                //delete all host entries!
            //}
            //catch (StorageException ex)
            //{
            //    Exception exception = new Exception(@"Table name: " + _deploymentLabel + @", is not allowed. Please check at http://msdn.microsoft.com/library/azure/dd135715.aspx", ex);
            //    throw exception;
            //}
        }

        public void WriteJob(JobItem job)
        {
            var configTable = tableClient.GetTableReference(string.Format("{0}{1}", _deploymentLabel, ConfigSuffix));

            var jobsIds = (from entity in configTable.CreateQuery<JobItem>()
                        where entity.RowKey.CompareTo(JobRowKey + "0") >= 0
                        &&
                        entity.RowKey.CompareTo(JobRowKey + "99999") < 0
                        select entity.RowKey).ToList();
            int highestIdNumber = 0;

            if (jobsIds.Any())
            {
                //get latest id
                var highestId = jobsIds.OrderByDescending(x => x).First();

                //not very robust - may yield error!
                //highestIdNumber = Convert.ToInt32(Regex.Match(highestId, JobRowKey + "(?<number>\\d{1,4})").Groups["number"].Value);
                highestIdNumber = Convert.ToInt32(highestId.Remove(0, 4));
            }
            job.RowKey = JobRowKey + (highestIdNumber+1);
            job.PartitionKey = string.Empty;
            configTable.Execute(TableOperation.InsertOrReplace(job));
        }

        public List<HostInformation> GetHosts()
        {
            var configTable = tableClient.GetTableReference(string.Format("{0}{1}", _deploymentLabel, ConfigSuffix));

            var hosts = from entity in configTable.CreateQuery<HostInformation>()
                        where entity.RowKey.CompareTo(HostStatusRowKey + "0") >= 0
                        &&
                        entity.RowKey.CompareTo(HostStatusRowKey + "99999") < 0
                        select entity;

            return hosts.ToList().Where(x => x.RowKey.StartsWith(HostStatusRowKey)).ToList();
        }


        public JobItem GetNextJob()
        {
            var jobs = GetJobs();

            try
            {
                return jobs.Where(x => !x.Finished).OrderBy(x => x.RowKey).First();
            }
            catch
            {
                return null;
            }
        }

        public List<JobItem> GetJobs()
        {
            var configTable = tableClient.GetTableReference(string.Format("{0}{1}", _deploymentLabel, ConfigSuffix));

            var jobs = (from entity in configTable.CreateQuery<JobItem>()
                        where entity.RowKey.CompareTo(JobRowKey + "0") >= 0
                        &&
                        entity.RowKey.CompareTo(JobRowKey + "99999") < 0
                        select entity).ToList();

            return jobs;
           
        }

        public void SetJobState(string jobId, bool finished, string resultFile)
        {
            var configTable = tableClient.GetTableReference(string.Format("{0}{1}", _deploymentLabel, ConfigSuffix));

            var jobs = from entity in configTable.CreateQuery<JobItem>()
                       where entity.RowKey == jobId
                       select entity;

            if (jobs != null)
            {
                var job = jobs.First();
                job.Finished = finished;
                job.ResultFileName = resultFile;
                configTable.Execute(TableOperation.Replace(job));
            }

        }

        public void SetHostStatus(int hostIndex, string address, List<string> readyJobs)
        { 
            var configTable = tableClient.GetTableReference(string.Format("{0}{1}", _deploymentLabel, ConfigSuffix));
            configTable.CreateIfNotExists();
            var rowkey = HostStatusRowKey + hostIndex;

            var hosts = (from entity in configTable.CreateQuery<HostInformation>()
                        where entity.RowKey.Equals(rowkey)
                        select entity).ToList(); //do not lazy load, therefore the "ToList"..
            
            HostInformation host;
            if (hosts.Any())
            {
                host = hosts.Single();
            }
            else
            {
                host = new HostInformation() { RowKey = rowkey };
                host.PartitionKey = string.Empty;
            }

            host.ReadyJobs = readyJobs.Any() ?  readyJobs.Aggregate((x, y) => x + "," + y) : "";
            host.InstanceIndex = hostIndex;
            host.Address = address;

            configTable.Execute(TableOperation.InsertOrReplace(host));
                    
        }


        public void ClearHostStates()
        {
            var configTable = tableClient.GetTableReference(string.Format("{0}{1}", _deploymentLabel, ConfigSuffix));
            configTable.CreateIfNotExists();
     
            var hosts = (from entity in configTable.CreateQuery<HostInformation>()
                         where entity.RowKey.CompareTo(HostStatusRowKey + "0") >= 0
                         select entity).ToList(); //do not lazy load, therefore the "ToList"..

            var operations = new TableBatchOperation();
            hosts.ForEach(x=>operations.Add(TableOperation.Delete(x)));

            configTable.ExecuteBatch(operations);

        }

        public Uri UploadBlob(string blobName, string fileName)
        {
            var blobContainer = blobClient.GetContainerReference(_deploymentLabel);
            blobContainer.CreateIfNotExists();

            var blob = blobContainer.GetBlockBlobReference(blobName);

            blob.UploadFromFile(fileName, System.IO.FileMode.Open);

            return blob.Uri;
        }

        public void DownloadBlob(string blobName, string fileName)
        {
            var blobContainer = blobClient.GetContainerReference(_deploymentLabel);
            var blob = blobContainer.GetBlockBlobReference(blobName);

            blob.DownloadToFile(fileName, System.IO.FileMode.Create);
        }

        
        public void WriteLog(string text)
        {
            var logTable = tableClient.GetTableReference(string.Format("{0}{1}", _deploymentLabel, LogSuffix));
            logTable.CreateIfNotExists();

            var logentry = new LogEntry()
            {
                HostName = Environment.MachineName,
                RowKey = Environment.MachineName+DateTime.Now.Ticks.ToString(),
                PartitionKey = String.Empty,
                Text = text

            };

            logTable.Execute(TableOperation.Insert(logentry));
        }

    }
}
