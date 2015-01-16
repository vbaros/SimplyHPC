using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSR.AzureEE.HpcWorkerRole
{
    public class InstanceIdHelper
    {
        public static int CurrentInstanceIndex
        {
            get
            {
                int instanceIndex;
                if (!int.TryParse(CurrentInstanceName.Substring(CurrentInstanceName.LastIndexOf("_") + 1), out instanceIndex)) // On cloud.
                {
                    int.TryParse(CurrentInstanceName.Substring(CurrentInstanceName.LastIndexOf(".") + 1), out instanceIndex); // On compute emulator.
                }
                return instanceIndex;
            }
        }

        public static string CurrentInstanceName
        {
            get
            {
               return  Environment.GetEnvironmentVariable("RoleInstanceID");
            }
        }

        public static string CurrentDeploymentLabel
        {
            get
            {
                return CloudConfigurationManager.GetSetting("HSR.DeploymentName");
            }
        }

        public static string CurrentDeploymentStorageAccountName
        {
            get
            {
                return CloudConfigurationManager.GetSetting("HSR.StorageAccountName");
            }
        }

        public static string CurrentDeploymentStorageAccountKey
        {
            get
            {
                return CloudConfigurationManager.GetSetting("HSR.StorageAccountKey");
            }
        }

    }
}
