﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using HSR.AzureEE.Controller;
using HSR.AzureEE.Controller.Impl;

namespace HSR.AzureEE.Module.Cmdlets
{
    [Cmdlet(VerbsCommon.Remove, "AzureService")]
    public class RemoveAzureService : Cmdlet
    {
        // Declare the parameters for the cmdlet.
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
        public string AzureServiceName
        {
            get { return _azureServiceName; }
            set { _azureServiceName = value; }
        }

        private AzureParameters _azureParameters;
        private string _azureServiceName;

        #region Cmdlet Overrides

        protected override void ProcessRecord()
        {
            // prepare objects
            var subscription = new AzureSubscription()
            {
                SubscriptionId = _azureParameters.parameters.SubscriptionID,
                ManagementCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                    _azureParameters.parameters.PathToManagementCertificate,
                    _azureParameters.parameters.CertificateEncryptedPassword),
                StorageAccount = _azureParameters.parameters.StorageAccountName,
                StorageAccountKey = _azureParameters.parameters.StorageAccountKey
            };

            // create new Azure controller
            AzureCloudController controller = new AzureCloudController(subscription, _azureParameters.parameters.AffinityGroupName);

            // delete Azure deployment
            controller.DeleteCloudService(_azureServiceName);
        }

        #endregion
    }
}
