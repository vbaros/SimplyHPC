using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace HSR.AzureEE.MpiWrapper
{
    public enum RunnerState
    {
        Idle,
        Running,
        Finished,
    }
    
    [ServiceContract]
    public interface IMPIRunner
    {
        [OperationContract]
        int RunApplication(string executable, string parameters, string[] hosts = null, int numCores = 1, int nHosts = 0);

        [OperationContract]
        string GetCurrentStandardOutput();

        [OperationContract]
        RunnerState GetState();

        [OperationContract]
        string GetResultFilePath();

    }
}
