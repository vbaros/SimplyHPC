using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSR.AzureEE.Controller
{
    public interface ICloudController
    {
        void CreateCloudService(string name);
        void StartCloudService(string name, Uri blobUri, string configurationPath);
        void DeleteCloudService(string name);
        List<InstanceState> GetCloudServiceState(string name);
    }
}
