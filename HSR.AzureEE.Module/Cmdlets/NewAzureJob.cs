using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Management.Automation;
using Ionic.Zip;
using System.IO;
using HSR.AzureEE.Controller.Storage;
using HSR.AzureEE.Controller;
using HSR.AzureEE.Controller.Impl;

namespace HSR.AzureEE.Module.Cmdlets
{
    [Cmdlet(VerbsCommon.New, "AzureJob")]
    public class NewAzureJob : Cmdlet
    {
        // Declare the parameters for the cmdlet.
        #region cmdlet parameters

        [Parameter(Mandatory = true, ValueFromPipeline = true, ValueFromPipelineByPropertyName = true, Position = 0,
            HelpMessage = "Azure Parameters returned from New-AzureService.")]
        [ValidateNotNullOrEmpty]
        public AzureParameters AzureParameters
        {
            get { return _azureParameters; }
            set { _azureParameters = value; }
        }

        [Parameter(Mandatory = true, Position = 1)]
        [ValidateNotNullOrEmpty]
        public string ClusterName
        {
            get { return _clusterName; }
            set { _clusterName = value; }
        }

        [Parameter(Mandatory = true, Position = 2)]
        [ValidateNotNullOrEmpty]
        public string JobDataDirectoryPath
        {
            get { return _jobDataDirectoryPath; }
            set { _jobDataDirectoryPath = value; }
        }

        [Parameter(Mandatory = true, Position = 3)]
        [ValidateNotNullOrEmpty]
        public string ExecutableFileName
        {
            get { return _executableFileName; }
            set { _executableFileName = value; }
        }

        [Parameter(Mandatory = false, Position = 4)]
        public string ExecutableArguments
        {
            get { return _executableArguments; }
            set { _executableArguments = value; }
        }

        [Parameter(Mandatory = false, Position = 5)]
        public int CoresPerNode
        {
            get { return _coresPerNode; }
            set { _coresPerNode = value; }
        }

        [Parameter(Mandatory = false, Position = 6)]
        public int JobType
        {
            get { return _jobType; }
            set { _jobType = value; }
        }

        private AzureParameters _azureParameters;
        private string _clusterName;
        private string _jobDataDirectoryPath;
        private string _executableFileName;
        private string _executableArguments = "";
        private int _coresPerNode = 1;
        private int _jobType = (int)JobItem.Type.MPI; // MPI is the default job type

        #endregion

        private bool _parametersValid = false;

        #region Cmdlet Overrides

        protected override void BeginProcessing()
        {
            // Verify all arguments

            // Check if exe file exists in the directory
            string executablePath = Path.Combine(_jobDataDirectoryPath, _executableFileName);

            if (!File.Exists(executablePath))
            {
                PSArgumentException ex = new PSArgumentException("File " + _executableFileName + " doesn't exist in the folder " + _jobDataDirectoryPath + ". Processing can't continue");
                WriteError(new ErrorRecord(ex, "Argument Exception", ErrorCategory.OpenError, executablePath));

                return;
            }

            _parametersValid = true;
        }

        protected override void ProcessRecord()
        {
            if (_parametersValid)
            {
                // generate temp zip file in users %temp% dir and zip the project dir
                Random r = new Random();
                string zipPath = Path.GetTempPath() + "job" + r.Next(1, 16777216).ToString() + ".zip";

                ZipUp (zipPath, _jobDataDirectoryPath);

                // create storage helper
                var subscription = new AzureSubscription()
                {
                    SubscriptionId = _azureParameters.parameters.SubscriptionID,
                    ManagementCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(
                        _azureParameters.parameters.PathToManagementCertificate,
                        _azureParameters.parameters.CertificateEncryptedPassword),
                    StorageAccount = _azureParameters.parameters.StorageAccountName,
                    StorageAccountKey = _azureParameters.parameters.StorageAccountKey
                };

                AzureStorageHelper storage = new AzureStorageHelper(subscription, _clusterName);
                AzureCloudController controller = new AzureCloudController(subscription, _azureParameters.parameters.AffinityGroupName);

                AzureDynamicCluster cluster = new AzureDynamicCluster(storage, controller);

                // upload zip with exectable and data, and run the job (automaticaly from Azure Worker role)
                try
                {
                    string jobID = cluster.SubmitJob(new JobItem()
                    {
                        CorePerNode = _coresPerNode, //needs to be adjusted in the future
                        Executable = _executableFileName,
                        Parameters = _executableArguments,
                        NumNodes = _azureParameters.parameters.NumberOfNodes,
                        InfoTag = "test",
                        JobType = _jobType
                    },
                    zipPath);

                    // delete temp zip file;
                    File.Delete(zipPath);

                    // write true to the pipeline indicating successful upload
                    WriteObject(jobID);
                }
                catch (Exception ex)
                {
                    // delete temp zip file;
                    File.Delete(zipPath);

                    throw ex;
                }
            }
            else
                WriteObject(null);
        }

        #endregion

        public static void SaveProgress(object sender, SaveProgressEventArgs e)
        {
            if (e.EventType == ZipProgressEventType.Saving_BeforeWriteEntry)
            {
                // write on the same line
                Console.CursorLeft = 0;
                Console.CursorTop--;

                Console.WriteLine("Compressing job data: {0}% Completed.", (int)(e.EntriesSaved*100.0/e.EntriesTotal));
            }
            else if (e.EventType == ZipProgressEventType.Saving_Completed)
            {
                // write on the same line
                Console.CursorLeft = 0;
                Console.CursorTop--;

                Console.WriteLine("Compressing job data: 100% Completed.");
            }
        }

        public static void ZipUp (string targetZip, string directory)
        {
            Console.CursorTop++;
            Console.CursorVisible = false;

            using (var zip = new ZipFile())
            {
                zip.SaveProgress += SaveProgress;
                zip.AddDirectory(directory);
                zip.Save(targetZip);
            }

            Console.CursorVisible = true;
        }
    }
}
