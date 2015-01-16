using HSR.AzureEE.Controller;
using HSR.AzureEE.Controller.Impl;
using HSR.AzureEE.Controller.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace HSR.AzureEE.cmd
{
    class Program
    {
        static void Main(string[] args)
        {
            ////////////////////////////////////////////////// CONFIGURATION /////////////////////////////////////////
            //Subscription CONFIGURATION
            const string subscriptionId = @"";
            const string pathToManagementCertificate = @"";
            const string mgmtCertificatePassword = @"";
            const string storageAccountName = @"";
            const string affinityGroupName = @"";
            const string storageAccountKey = @"";

            //Cluster Configuration
            Console.WriteLine("Please enter a name for the cluster (only alphanumeric characters):");
            var clusterName = Console.ReadLine();

            Console.WriteLine("Please specify the size of the nodes (e.g. Small):");
            var nodeSize = Console.ReadLine();

            Console.WriteLine("Please enter the number of nodes to create:");
            var nodeNumber = Int32.Parse(Console.ReadLine());

            Console.WriteLine("Please enter the path to a deployment template");
            var pathToTempl = Console.ReadLine();


            ////////////////////////////////////////////////// CREATION /////////////////////////////////////////
            // prepare objects
            var subscription  = new AzureSubscription()
            {
                SubscriptionId = subscriptionId,
                ManagementCertificate = new System.Security.Cryptography.X509Certificates.X509Certificate2(pathToManagementCertificate, mgmtCertificatePassword),
                StorageAccount = storageAccountName,
                StorageAccountKey = storageAccountKey
            };
            var controller = new AzureCloudController(subscription, affinityGroupName);
            var storage = new AzureStorageHelper(subscription, clusterName);
            var template = new AzureDeploymentTemplate(pathToTempl);


            var cluster = new AzureDynamicCluster(storage, controller);
            cluster.Initialize(clusterName, storageAccountName, storageAccountKey, new AzureInstanceType() { Name = nodeSize }, nodeNumber, template);

            //create Cluster
            Console.WriteLine("Creating cluster..");
            cluster.CreateCluster();
            Console.WriteLine("Creating cluster finished.. Please wait until it is ready (may take about 10 min)");


            bool clusterReady = false;
            while (!clusterReady)
            {
                Thread.Sleep(5000);
                var state = cluster.GetState();
                if (!state.ClusterReady)
                {
                    Console.WriteLine("Cluster not ready:");
                    foreach (var instanceState in state.InstanceStates)
                    {
                        Console.WriteLine("Instance State = " + instanceState.AzureStateName);
                        Console.WriteLine("Instance State Details = " + instanceState.AzureStateDetails ?? "N/A");
                    }
                    Console.WriteLine();

                }
                else
                {
                    clusterReady = true;
                    Console.WriteLine("Cluster ready!!");
                }
            }


            ////////////////////////////////////////////////// Run Jobs /////////////////////////////////////////
            bool exit = false;
            while (!exit)
            {
                Console.WriteLine("If you want to set up a new job, enter \"job\", to delete the cluster enter \"delete\", to see all job states enter \"print\"");
                var answer = Console.ReadLine();

                if (answer == "delete")
                {
                    exit = true;
                    continue;
                }

                if (answer == "print")
                {
                    foreach (JobItem job in cluster.GetJobs())
                    {
                        Console.WriteLine("job " + job.RowKey + " " + (job.Finished ? "finished" : "not finished") + " result path = " + (job.Finished ? job.ResultFileName : "N/A"));

                    }
                
                    continue;
                }

                if (answer == "job")
                {
                    Console.WriteLine("Please enter the path to the ZIP File containing all job data");
                    string pathToZip = Console.ReadLine();

                    Console.WriteLine("Please enter the name of the Executable (eg. petsc.exe)");
                    string exec = Console.ReadLine();

                    Console.WriteLine("Please enter all command line arguments");
                    string arguments = Console.ReadLine();

                    var jobId = cluster.SubmitJob(new JobItem()
                       {
                           CorePerNode = 1, //needs to be adjusted in the future
                           Executable = exec,
                           Parameters = arguments,
                           NumNodes = nodeNumber,
                           InfoTag = "test",
                           JobType = (int)JobItem.Type.MPI
                       },
                      pathToZip);

                    //get job results
                    Console.WriteLine(@"Assigned job " + jobId);

                    while (!cluster.HasJobFinished(jobId))
                    {
                        Thread.Sleep(5000);
                        Console.WriteLine("Job " + jobId + " running/waiting to run...");

                    }

                    Console.WriteLine("Downloading job results");
                    bool downloaded = cluster.DownloadJobResults(jobId);

                    if (downloaded)
                        Console.WriteLine("Job results stored in file " + cluster.GetJobs().Where(x => x.RowKey == jobId).First().ResultFileName);
                    else
                        Console.Error.WriteLine("Error! Job result was not created");
                }
               
            }

             ////////////////////////////////////////////////// CLEAN UP /////////////////////////////////////////
            Console.WriteLine("Finished, press enter to delete all deployments");
            Console.ReadLine();
            cluster.DeleteCluster();

            
        }
    }
}
