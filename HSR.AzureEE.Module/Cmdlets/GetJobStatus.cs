using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using HSR.AzureEE.Controller;
using HSR.AzureEE.Controller.Storage;
using HSR.AzureEE.Controller.Impl;

namespace HSR.AzureEE.Module.Cmdlets
{
    [Cmdlet(VerbsCommon.Get, "JobStatus")]
    public class GetJobStatus : Cmdlet
    {
        #region cmdlet parameters

        [Parameter(Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, Position = 0,
             HelpMessage = "Azure Parameters returned from New-AzureService.")]
       [ValidateNotNullOrEmpty]
        public AzureParameters AzureParameters
        {
            get { return _azureParameters; }
            set { _azureParameters = value; }
        }

        [Parameter(Mandatory = true, Position = 1)]
        [ValidateNotNullOrEmpty]
        public string ClusterName
        {
            get { return _clusterName; }
            set { _clusterName = value; }
        }

        [Parameter(Mandatory = true, Position = 2)]
        [ValidateNotNullOrEmpty]
        public string JobID
        {
            get { return _jobID; }
            set { _jobID = value; }
        }

        private AzureParameters _azureParameters;
        private string _clusterName;
        private string _jobID;

        #endregion

        private volatile bool _stopCheckingJobStatus = false;

        #region cmdlet overrides

        protected override void ProcessRecord()
        {
            // create storage helper
            var subscription = new AzureSubscription()
            {
                SubscriptionId = _azureParameters.parameters.SubscriptionID,
                ManagementCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                    _azureParameters.parameters.PathToManagementCertificate,
                    _azureParameters.parameters.CertificateEncryptedPassword),
                StorageAccount = _azureParameters.parameters.StorageAccountName,
                StorageAccountKey = _azureParameters.parameters.StorageAccountKey
            };

            AzureStorageHelper storage = new AzureStorageHelper(subscription, _clusterName);
            AzureCloudController controller = new AzureCloudController(subscription, _azureParameters.parameters.AffinityGroupName);

            AzureDynamicCluster cluster = new AzureDynamicCluster(storage, controller);

            if (!_stopCheckingJobStatus)
            {
                // return true throught the pipeline if job is finished
                WriteObject(cluster.HasJobFinished(_jobID));
            }
        }

        protected override void StopProcessing()
        {
            _stopCheckingJobStatus = true;
        }
        
        #endregion
    }
}
