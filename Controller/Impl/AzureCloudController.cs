using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Management.Compute;
using Microsoft.WindowsAzure.Management.Compute.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Management;
using Microsoft.WindowsAzure.Management.Models;

namespace HSR.AzureEE.Controller.Impl
{
    /// <summary>
    /// Contains all methods to communicate with Azure's API
    /// </summary>
    public class AzureCloudController  : ICloudController, ICloudManagement
    {

        private AzureSubscription _subscription;
        private SubscriptionCloudCredentials _credentials;
        private string _affinityGroup;
      
        public AzureCloudController(AzureSubscription subscription, string affinityGroup)
        {
            _subscription = subscription;
            _credentials = new CertificateCloudCredentials(subscription.SubscriptionId, subscription.ManagementCertificate);
            
            _affinityGroup = affinityGroup;
        }


        public void CreateCloudService(string name)
        {
            using (ComputeManagementClient client =
                CloudContext.Clients.CreateComputeManagementClient(_credentials))
            {
                //create service
                client.HostedServices.Create(
                    new HostedServiceCreateParameters
                    {
                        ServiceName = name,
                        AffinityGroup = _affinityGroup
                        });

                //add HSR default certificate (hsrRDP.pfx)
                //get certificate from embedded resources
                client.ServiceCertificates.Create(name, new ServiceCertificateCreateParameters()
                {
                    CertificateFormat = CertificateFormat.Pfx,
                    Password = Properties.Resources.pass, //not very sensitive information... 
                    Data = Properties.Resources.hsrRdp //included in resources, see project properties/Resources!
                });
            }
        }

        /// <summary>
        /// Starts a previously created Cloudservice with a Template
        /// 
        /// BILLING Starts here!
        /// </summary>
        /// <param name="name">Name of the cloudservice</param>
        /// <param name="blobUri">Path to the template on azure Blob Storage</param>
        /// <param name="configurationPath">Local Path to a configuration file</param>
        public void StartCloudService(string name, Uri blobUri, string configurationPath)
        {
            using (ComputeManagementClient client =
            CloudContext.Clients.CreateComputeManagementClient(_credentials))
            {


               client.Deployments.Create(name,
                    DeploymentSlot.Production,
                    new DeploymentCreateParameters
                    {
                        Name = name + "Prod",
                        Label = name + "Prod",
                        PackageUri = blobUri,
                        Configuration = File.ReadAllText(configurationPath),
                        StartDeployment = true
                    });

       
            }

        }


        /// <summary>
        /// Deletes a cloud service and all its deployments
        /// 
        /// BILLING Stops here!
        /// </summary>
        /// <param name="name"></param>
        public void DeleteCloudService(string name)
        {
            using (ComputeManagementClient client =
              CloudContext.Clients.CreateComputeManagementClient(_credentials))
            {
                var answer = client.Deployments.DeleteBySlot(name, DeploymentSlot.Production);

                while (client.GetOperationStatus(answer.RequestId).Status != OperationStatus.Succeeded)
                {
                    Thread.Sleep(5000);
                }
                client.HostedServices.Delete(name);
            }
        }

        public List<InstanceState> GetCloudServiceState(string name)
        {
            using (ComputeManagementClient client =
            CloudContext.Clients.CreateComputeManagementClient(_credentials))
            {

                var deployment = client.HostedServices.GetDetailed(name).Deployments.Single(x => x.DeploymentSlot == DeploymentSlot.Production);
                
                return deployment.RoleInstances.Select(x => new InstanceState()
                {
                    AzureStateDetails = x.InstanceStateDetails,
                    AzureStateName = x.InstanceStatus
                }
                ).ToList();
            }
        }

        //public IList<RoleDescription> GetAvailableInstanceTypes()
        public RoleDescription [] GetAvailableInstanceTypes()
        {
            using (ManagementClient client =
                CloudContext.Clients.CreateManagementClient (_credentials))
            {
                IList<RoleSizeListResponse.RoleSize> available = client.RoleSizes.List().RoleSizes;

                RoleDescription [] availableRoles = new RoleDescription[available.Count];

                for (int i = 0; i < available.Count; i++ )
                {
                    availableRoles[i] = new RoleDescription {
                        Name = available[i].Name,
                        Label = available[i].Label,
                        VM = available[i].SupportedByVirtualMachines,
                        WorkerRole = available[i].SupportedByWebWorkerRoles
                    };
                }

                return availableRoles;
            }
        }

        public bool IsCloudServiceNameAvailable(string name)
        {
            using (ComputeManagementClient client =
                CloudContext.Clients.CreateComputeManagementClient(_credentials))
            {
                var available = client.HostedServices.CheckNameAvailability(name);

                return available.IsAvailable;
            }
        }
    }
}
