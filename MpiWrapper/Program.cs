using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HSR.AzureEE.MpiWrapper
{

    class Program
    {
        static void Main(string[] args)
        {
            IMPIRunner runner = new WCFMPIRunner();

            using (var host = new ServiceHost(new WCFMPIRunner(), new Uri("net.pipe://localhost")))
            {

                host.AddServiceEndpoint(typeof(IMPIRunner),
                                           new NetNamedPipeBinding(),
                                           "MPIRunner");


                host.Open();
                Console.WriteLine(@"MPIRunner started...");

                while (Console.ReadLine() != "exit")
                {
                    //run
                }

                host.Close();
            }
        }
    }
    

}
