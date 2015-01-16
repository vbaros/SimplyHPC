using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Microsoft.SqlServer.Server;

namespace HSR.AzureEE.Controller.Impl
{
	public class AzureDeploymentTemplate : IDeploymentTemplate
	{
		// xml specifications
		private const string ServiceDefinitionFileName = "ServiceDefinition.csdef";
		private const string ServiceDefinitionNamespaceUri = "http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceDefinition";
		private const string VmSizeXmlPath = "/x:ServiceDefinition/x:WorkerRole";
		private const string VmSizeXmlAttributeName = "vmsize";

		private const string ServiceConfigurationFileName = "ServiceConfiguration.Cloud.cscfg";
		private const string ServiceConfigurationNamespaceUri = ("http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration");
		private const string InstanceCountXmlPath = "/x:ServiceConfiguration/x:Role/x:Instances";
		private const string InstanceCountXmlAttributeName = "count";

        private const string DeploymentNameXmlPath = "/x:ServiceConfiguration/x:Role/x:ConfigurationSettings/x:Setting[@name='HSR.DeploymentName']";
        private const string DeploymnetNameXmlAttributeName = "value";

        private const string StorageAccountNameXmlPath = "/x:ServiceConfiguration/x:Role/x:ConfigurationSettings/x:Setting[@name='HSR.StorageAccountName']";
        private const string StorageAccountNameXmlAttributeName = "value";

        private const string StorageAccountKeyXmlPath = "/x:ServiceConfiguration/x:Role/x:ConfigurationSettings/x:Setting[@name='HSR.StorageAccountKey']";
        private const string StorageAccountKeyXmlAttributeName = "value";

		// cspack specifications
        private string _cspackExePath = @"cspack\cspack.exe"; //relative path, because cspack is included next to this dll
        private const string RoleName = "HSR.AzureEE.HpcWorkerRole";

		// 
		private string _templateDirectory;
		private string _serviceConfigurationFile;
		private string _serviceDefinitionFile;
		
		/******************************* AzureDeplymentTemplate *******************************/
		
		public AzureDeploymentTemplate(string templateDirectory)
		{
			_templateDirectory = templateDirectory;
			_serviceDefinitionFile = Path.Combine(_templateDirectory, ServiceDefinitionFileName);
			_serviceConfigurationFile = Path.Combine(_templateDirectory, ServiceConfigurationFileName);
			
			checkPaths();
		}

		/************************************** Customize *************************************/

		public void Customize(string deploymentName, string storageAccountname, string storageAccountKey, AzureInstanceType instanceType, int instanceCount)
		{
			checkPaths();
            
			SetXmlAttribute(_serviceDefinitionFile, ServiceDefinitionNamespaceUri, VmSizeXmlPath, VmSizeXmlAttributeName, instanceType.Name);
			SetXmlAttribute(_serviceConfigurationFile, ServiceConfigurationNamespaceUri, InstanceCountXmlPath, InstanceCountXmlAttributeName, instanceCount.ToString());
            SetXmlAttribute(_serviceConfigurationFile, ServiceConfigurationNamespaceUri, DeploymentNameXmlPath, DeploymnetNameXmlAttributeName, deploymentName);
		
            // storage acccount
            SetXmlAttribute(_serviceConfigurationFile, ServiceConfigurationNamespaceUri, StorageAccountNameXmlPath, StorageAccountNameXmlAttributeName, storageAccountname);
            SetXmlAttribute(_serviceConfigurationFile, ServiceConfigurationNamespaceUri, StorageAccountKeyXmlPath, StorageAccountKeyXmlAttributeName, storageAccountKey);
        }
		
		private static void SetXmlAttribute(string file, string xmlNamespaceUri, string xmlPath, string xmlAttribute, string value)
		{
			// load file to XDocument
			var xDocument = XDocument.Load(file);

			// create namespace manager for default namespace
			var nsManager = new XmlNamespaceManager(new NameTable());
			nsManager.AddNamespace("x", xmlNamespaceUri);

			// select element
			var xElement = xDocument.XPathSelectElement(xmlPath, nsManager);
			if (xElement == null)
				{
					throw new XmlException(string.Format("XML not found. XML path not valid or file not structured accordingly. file: {0}, XML path: {1}", file, xmlPath));
				}

			// set attribute
			xElement.SetAttributeValue(xmlAttribute, value);

			// save xml to file
			xDocument.Save(file);
		}

		/**************************************** Pack ****************************************/

		public void Pack(string csPackFileName, string csCfgFileName)
		{
			checkPaths();

			if (!File.Exists(_cspackExePath))
			{
                // get absolute path to cspack.exe next to the dll
                _cspackExePath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), _cspackExePath);

                if (!File.Exists(_cspackExePath))
                    throw new FileNotFoundException("Cspack executable not found at: " + _cspackExePath);
			}   

			// set process specifications for packing
			var cspackProcessInfo = new ProcessStartInfo(_cspackExePath);
            var roleDirectory = _templateDirectory;// Path.Combine(_templateDirectory, RoleName);
            cspackProcessInfo.Arguments = String.Format("/role:{0};{1};{0}.dll {2} /out:{3}", RoleName, roleDirectory, _serviceDefinitionFile, csPackFileName);
			//cspackProcessInfo.CreateNoWindow = true;
			cspackProcessInfo.UseShellExecute = false;
			
			// start process and wait for it to exit or reach the timeout
			var cspackProcess = Process.Start(cspackProcessInfo);
			cspackProcess.WaitForExit(60000);
            
            if (cspackProcess.ExitCode != 0)
            {
                throw new Exception("CSPACK exitcode error: " + cspackProcess.ExitCode);
            }

            //copy csDef to correct place
            File.Copy(_serviceConfigurationFile, csCfgFileName, true);
		}
	
		/**************************************** Common ***************************************/

		private void checkPaths()
		{
			if (!Directory.Exists(_templateDirectory))
			{
				throw new DirectoryNotFoundException("Template directory not found: " + _templateDirectory);
			}

			if (!File.Exists(_serviceDefinitionFile))
			{
				throw new FileNotFoundException("Service definition file not found in template directory: " + _serviceDefinitionFile);
			}

			if (!File.Exists(_serviceConfigurationFile))
			{
				throw new FileNotFoundException("Service configuration file not found in template directory: " + _serviceConfigurationFile);
			}
		}

		/**************************************************************************************/

	}
}