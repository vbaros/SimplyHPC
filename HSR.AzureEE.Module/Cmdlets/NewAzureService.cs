using HSR.AzureEE.Controller;
using HSR.AzureEE.Controller.Impl;
using HSR.AzureEE.Controller.Storage;
using System;
using System.Security;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using System.IO;
using System.Xml;
using System.Diagnostics;

namespace HSR.AzureEE.Module.Cmdlets
{
    [Cmdlet(VerbsCommon.New, "AzureService")]
    public class NewAzureService : PSCmdlet
    {
        // Parameters
        // 01   xmlfile path to the configuration file
        // 02   Azure Service name
        // 03   Size of the nodes (e.g. Small):
        // 04   Number of nodes to create

        #region Cmdlet parameters

        // Declare the parameters for the cmdlet.
        [Parameter(Mandatory = true, Position = 0,
              HelpMessage = "XML file containing valid Azure subscription information.")]
        [ValidateNotNullOrEmpty]
        public string XMLFilePath
        {
            get { return _xmlFilePath; }
            set { _xmlFilePath = value; }
        }

        [Parameter(Mandatory = true, Position = 1)]
        [ValidateNotNullOrEmpty]
        public string AzureServiceName
        {
            get { return _azureServiceName; }
            set { _azureServiceName = value; }
        }

        [Parameter(Mandatory = true, Position = 2)]
        [ValidateNotNullOrEmpty]
        public string SizeOfNodes
        {
            get { return _sizeOfNodes; }
            set { _sizeOfNodes = value; }
        }

        [Parameter(Mandatory = true, Position = 3)]
        [ValidateNotNullOrEmpty]
        public int NumberOfNodes
        {
            get { return _numberOfNodes; }
            set { _numberOfNodes = value; }
        }

        [Parameter(Mandatory = true, Position = 4)]
        [ValidateNotNullOrEmpty]
        public string PathToDeploymentTemplate
        {
            get { return _pathToDeploymentTemplate; }
            set { _pathToDeploymentTemplate = value; }
        }

        private string _subscriptionId;
        private string _pathToManagementCertificate;
        private SecureString _mgmtCertificateEncryptedPassword;
        private string _storageAccountName;
        private string _affinityGroupName;
        private string _storageAccountKey;

        #endregion Cmdlet parameters

        private string _xmlFilePath;
        private string _azureServiceName;
        private string _sizeOfNodes;
        private int _numberOfNodes;
        private string _pathToDeploymentTemplate;

        #region Cmdlet Overrides

        private volatile bool _serviceCreatedAndReady = false;
        private bool _parametersValid = false;

        protected override void BeginProcessing()
        {
            // Does XML exist?
            // SessionState.Path contains the path from where the cmdlet was executed
            if (!Path.IsPathRooted(_xmlFilePath))
            {
                _xmlFilePath = Path.Combine(SessionState.Path.CurrentFileSystemLocation.Path, _xmlFilePath);
            }

            if (!File.Exists(_xmlFilePath))
            {
                PSArgumentException ex = new PSArgumentException("Configuration XML at path: " + _xmlFilePath + " doesn't exist. Processing can't continue");
                WriteError(new ErrorRecord(ex, "Argument Exception", ErrorCategory.OpenError, _xmlFilePath));

                return;
            }
            else
                ReadConfigurationXML(_xmlFilePath);

            // Validate XML parameters

            if (_subscriptionId.Length == 0 |
                _pathToManagementCertificate.Length == 0 |
                _mgmtCertificateEncryptedPassword.Length == 0 |
                _storageAccountName.Length == 0 |
                _affinityGroupName.Length == 0 |
                _storageAccountKey.Length == 0
                )
            {
                PSArgumentException ex = new PSArgumentException("Configuration XML file contains invalid data. Processing can't continue");
                WriteError(new ErrorRecord(ex, "Argument Exception", ErrorCategory.InvalidData, _xmlFilePath));

                return;
            }

            _parametersValid = true;
        }

        protected override void ProcessRecord()
        {
            // Create Azure Service
            if (_parametersValid)
            {
                // prepare objects
                var subscription = new AzureSubscription()
                {
                    SubscriptionId = _subscriptionId,
                    ManagementCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(_pathToManagementCertificate, _mgmtCertificateEncryptedPassword),
                    StorageAccount = _storageAccountName,
                    StorageAccountKey = _storageAccountKey
                };

                AzureCloudController controller = new AzureCloudController(subscription, _affinityGroupName);

                // check if azure service name is available
                bool available = controller.IsCloudServiceNameAvailable(_azureServiceName);

                if (!available)
                {
                    PSArgumentException ex = new PSArgumentException("Azure Service name: " + _azureServiceName + " already exists.");
                    WriteError(new ErrorRecord(ex, "Argument Exception", ErrorCategory.InvalidData, _azureServiceName));

                    return;
                }

                AzureStorageHelper storage = new AzureStorageHelper(subscription, _azureServiceName);
                AzureDeploymentTemplate template = new AzureDeploymentTemplate(_pathToDeploymentTemplate);


                AzureDynamicCluster cluster = new AzureDynamicCluster(storage, controller);
                cluster.Initialize(_azureServiceName, _storageAccountName, _storageAccountKey, new AzureInstanceType() { Name = _sizeOfNodes }, _numberOfNodes, template);

                //create Cluster
                Console.WriteLine("Creating cluster. Please wait...");
                cluster.CreateCluster();
                Console.WriteLine("Creating cluster finished.");
                Console.WriteLine("Please wait until it is ready (may take up to 10 min.)");


                // remember Cursor possition
                int left = Console.CursorLeft;
                int top = Console.CursorTop;

                // initialize stopwatch
                Stopwatch sw = new Stopwatch();

                sw.Start();
                while (!_serviceCreatedAndReady)
                {
                    Thread.Sleep(5000);
                    var state = cluster.GetState();
                    if (!state.ClusterReady)
                    {
                        Console.CursorLeft = left;
                        Console.CursorTop = top;

                        Console.WriteLine("Cluster not ready:");
                        foreach (var instanceState in state.InstanceStates)
                        {
                            Console.WriteLine("Instance State = " + instanceState.AzureStateName + "          ");
                            Console.WriteLine("Instance State Details = " + (instanceState.AzureStateDetails ?? "N/A"));
                        }

                        Console.WriteLine(string.Format("Elapsed time: {0}:{1}", Math.Floor(sw.Elapsed.TotalMinutes), sw.Elapsed.ToString("ss")));
                    }
                    else
                    {
                        _serviceCreatedAndReady = true;
                        Console.WriteLine();
                        Console.WriteLine("Cluster ready!");
                    }
                }

                sw.Stop();
            }
            else
            {
                WriteObject(null);
                return;
            }

            // initialize return object
            AzureParameters azureparameters = new AzureParameters()
            {
                parameters = new AzureParameters.Parameters()
                {
                    SubscriptionID = _subscriptionId,
                    PathToManagementCertificate = _pathToManagementCertificate,
                    CertificateEncryptedPassword = _mgmtCertificateEncryptedPassword,
                    StorageAccountName = _storageAccountName,
                    AffinityGroupName = _affinityGroupName,
                    StorageAccountKey = _storageAccountKey,
                    NumberOfNodes = _numberOfNodes
                },

                clusterready = _serviceCreatedAndReady
            };

            WriteObject(azureparameters);

        }

        protected override void StopProcessing()
        {
            // stop the loop in ProcessRecord
            _serviceCreatedAndReady = true;

            Console.WriteLine();
            Console.WriteLine("Ctrl-C pressed. Please check Azure portal and delete any deploments thay may be created.");
            Console.WriteLine("Exiting...");
        }

        #endregion Cmdlet Overrides

        private void ReadConfigurationXML(string XMLFilePath)
        {
            try
            {
                StringBuilder output = new StringBuilder();

                // load xml file
                XmlDocument doc = new XmlDocument();

                doc.Load(_xmlFilePath);
                string xmlString = doc.InnerXml;

                // Create an XmlReader
                using (XmlReader reader = XmlReader.Create(new StringReader(xmlString)))
                {
                    reader.ReadToFollowing("azure_configuration");
                    reader.ReadToFollowing("configuration");

                    reader.ReadStartElement();

                    while (reader.NodeType != XmlNodeType.EndElement)
                    {
                        switch (reader.Name)
                        {
                            case "SubscriptionID":
                                _subscriptionId = reader.ReadElementContentAsString();
                                break;

                            case "PathToManagementCertificate":
                                _pathToManagementCertificate = reader.ReadElementContentAsString();
                                break;

                            case "ManagementCertificatePassword":
                                _mgmtCertificateEncryptedPassword = GetEncryptedPassword.DecryptString(reader.ReadElementContentAsString());
                                break;

                            case "StorageAccountName":
                                _storageAccountName = reader.ReadElementContentAsString();
                                break;

                            case "StorageAccountKey":
                                _storageAccountKey = reader.ReadElementContentAsString();
                                break;

                            case "AffinityGroupName":
                                _affinityGroupName = reader.ReadElementContentAsString();
                                break;

                            default:
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteError (new ErrorRecord(ex, "Argument Exception", ErrorCategory.InvalidData, _xmlFilePath));
            }
        }
    }

    public struct AzureParameters
    {
        public struct Parameters
        {
            public string SubscriptionID { get; set; }
            public string PathToManagementCertificate { get; set; }
            public SecureString CertificateEncryptedPassword { get; set; }
            public string StorageAccountName { get; set; }
            public string AffinityGroupName { get; set; }
            public string StorageAccountKey { get; set; }
            public int NumberOfNodes { get; set; }
        };

        public Parameters parameters;

        public bool clusterready { get; set; }
    }
}
