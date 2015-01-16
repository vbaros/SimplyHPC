using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSR.AzureEE.Controller.Storage
{
    public enum HostState
    { 
        New,
        Error,
        Ready,
    }

    public class HostInformation : TableEntity
    {
        public int InstanceIndex { get; set; }
        public string Address { get; set; }
        public string State { get; set; }
        public string ReadyJobs { get; set; }
    }
}
