using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HSR.AzureEE.Controller.Storage;

namespace HSR.AzureEE.Controller
{
    public interface IAzureStorageAdapter
    {
	    Uri UploadBlob(string blobname, string sourceFileName);
        void DownloadBlob(string blobName, string targetFileName);

        void WriteConfiguration(DeploymentConfiguration config);
        void WriteJob(JobItem job);

        List<JobItem> GetJobs();
    }
}
