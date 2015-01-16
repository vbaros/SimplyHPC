using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSR.AzureEE.Controller.Storage
{
    public class LogEntry : TableEntity
    {
        public string HostName { get; set; }
        public string Text { get; set; }
    }
}
