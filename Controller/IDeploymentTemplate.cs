using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSR.AzureEE.Controller
{
    public interface IDeploymentTemplate
    {
		void Customize(string deploymentName, string storageaccountname, string storageaccountkey, AzureInstanceType instanceType, int instanceCount);
        void Pack(string csPackFileName, string csCfgFileName);

    }
}
