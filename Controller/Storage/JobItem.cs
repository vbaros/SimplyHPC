using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSR.AzureEE.Controller.Storage
{
    public class JobItem : TableEntity
    {
        public int CorePerNode { get; set; }
        public int NumNodes { get; set; }
        public string InfoTag { get; set; }
        public string Executable { get; set; }
        public string Parameters { get; set; }
        public string FileName { get; set; }
        public string ResultFileName { get; set; }
        public bool Finished { get; set; }
        public int JobType { get; set; }

        public enum Type
        {
            MPI = 0,
            Normal
        }
    }
}
