using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security;
using System.Threading.Tasks;
using System.Management.Automation;
using System.IO;
using System.Xml;

namespace HSR.AzureEE.Module.Cmdlets
{
    [Cmdlet(VerbsCommon.New, "AzureParameters")]
    public class NewAzureParameters : PSCmdlet
    {
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

        private string _xmlFilePath;

        #endregion Cmdlet parameters
        
        private string _subscriptionId;
        private string _pathToManagementCertificate;
        private SecureString _mgmtCertificateEncryptedPassword;
        private string _storageAccountName;
        private string _affinityGroupName;
        private string _storageAccountKey;

        private bool _parametersValid = false;

        #region Cmdlet Overrides

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
            // initialize return object
            if (_parametersValid)
            {
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
                        NumberOfNodes = 0
                    },

                    clusterready = false
                };

                WriteObject(azureparameters);
            }
            else
                WriteObject(null);
        }

        #endregion

        private void ReadConfigurationXML(string XMLFilePath)
        {
            try
            {
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
                WriteError(new ErrorRecord(ex, "Argument Exception", ErrorCategory.InvalidData, _xmlFilePath));
            }
        }
    }
}