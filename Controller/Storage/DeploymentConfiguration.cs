using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSR.AzureEE.Controller.Storage
{
    public class DeploymentConfiguration : TableEntity
    {
        public string DeploymentLabel { get; set; }
        public int InstanceCount { get; set; }
    }

}
