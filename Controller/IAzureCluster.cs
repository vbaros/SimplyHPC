using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HSR.AzureEE.Controller.Storage;

namespace HSR.AzureEE.Controller
{
    /*
     * Main Interface that supports all operations
     * to
     * - Create a deployment
     * - Use a deployment
     * and delete a deployment
     * 
     * 
     * 
     */ 
    public interface IAzureCluster
    {

        void Initialize(string configFilePath);
        //void Initialize(string name, string storageaccountname, string storageaccountkey, AzureInstanceType instanceType, int count, IDeploymentTemplate template);

        void CreateCluster();

        ClusterState GetState();

        string SubmitJob(JobItem job, string jobDataPath);
        bool HasJobFinished(string jobId);
        List<JobItem> GetJobs();

        void DeleteCluster();
        
    }
}
