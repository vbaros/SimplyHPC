using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Management;
using Microsoft.WindowsAzure.Management.Models;

namespace HSR.AzureEE.Controller
{
    interface ICloudManagement
    {
        //IList<RoleDescription> GetAvailableInstanceTypes();
        RoleDescription[] GetAvailableInstanceTypes();
        bool IsCloudServiceNameAvailable(string name);
    }

    public struct RoleDescription
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public bool VM { get; set; }
        public bool WorkerRole { get; set; }
    };
}
