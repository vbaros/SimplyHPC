using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace HSR.AzureEE.Controller
{
	public class AzureInstanceType
	{
		public string Name;
		public string Label;
		public int Cores;
		public int MemoryInMb ;
		public bool SupportedByWebWorkerRoles ;
		public bool SupportedByVirtualMachines ;
		public int MaxDataDiskCount ;
		public int WebWorkerResourceDiskSizeInMb ;
		public int VirtualMachineResourceDiskSizeInMb;

		// xml specifications
        //private const string RoleSizeNamespaceUri = "http://schemas.microsoft.com/windowsazure";


		// create a list of AzureInstanceType from xDocument
        //public static List<AzureInstanceType> ParseRoleSizeXDocument(XDocument xDoc)
        //{
        //    var nsManager = new XmlNamespaceManager(new NameTable());
        //    nsManager.AddNamespace("x", RoleSizeNamespaceUri);

        //    XNamespace ns = RoleSizeNamespaceUri;
			
        //    return xDoc.Root.Descendants(ns + "RoleSize").Select(x => ParseRoleSizeXElement(x, ns)).ToList();

        //}

        //private static AzureInstanceType ParseRoleSizeXElement(XElement element, XNamespace ns)
        //{
        //    var dict = element.Elements().ToDictionary(
        //        x => x.Name.LocalName,
        //        x => x.Value);

        //    string output;
        //    var instanceType = new AzureInstanceType();

        //    if (dict.TryGetValue("Name", out output))
        //    {
        //        instanceType.Name = output;
        //    }

        //    if (dict.TryGetValue("Label", out output))
        //    {
        //        instanceType.Label = output;
        //    }

        //    if (dict.TryGetValue("Cores", out output))
        //    {
        //        instanceType.Cores = Convert.ToInt32(output);
        //    }

        //    if(dict.TryGetValue("MemoryInMb", out output))
        //    {
        //        instanceType.MemoryInMb = Convert.ToInt32(output);
        //    }

        //    if(dict.TryGetValue("SupportedByWebWorkerRoles", out output))
        //    {
        //        instanceType.SupportedByWebWorkerRoles = Convert.ToBoolean(output);
        //    }

        //    if(dict.TryGetValue("SupportedByVirtualMachines", out output))
        //    {
        //        instanceType.SupportedByVirtualMachines = Convert.ToBoolean(output);
        //    }

        //    if(dict.TryGetValue("MaxDataDiskCoun", out output))
        //    {
        //        instanceType.MaxDataDiskCount = Convert.ToInt32(output);
        //    }

        //    if(dict.TryGetValue("WebWorkerResourceDiskSizeInMb", out output))
        //    {
        //        instanceType.WebWorkerResourceDiskSizeInMb = Convert.ToInt32(output);
        //    }

        //    if(dict.TryGetValue("VirtualMachineResourceDiskSizeInMb", out output))
        //    {
        //        instanceType.VirtualMachineResourceDiskSizeInMb = Convert.ToInt32(output);
        //    }

        //    return instanceType;

        //}
	}
}
