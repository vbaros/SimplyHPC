using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

namespace HSR.AzureEE.Controller
{
    public class AzureSubscription
    {
        public string SubscriptionId { get; set; }
        public X509Certificate2 ManagementCertificate { get; set; }
        public string StorageAccount { get; set; }
        public string StorageAccountKey { get; set; }
    }
}
