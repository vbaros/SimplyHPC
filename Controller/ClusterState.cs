using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HSR.AzureEE.Controller
{   
    public class InstanceState
    {
        public enum InstanceStateSummary
        {
            Preparing,
            Ready,
            Deleting,
            Problem,
            Unknown
        }

        private static string[] PreparingStates = {"CreatingVM", "StartingVM", "CreatingRole", "StartingRole", "BusyRole"};
        private static string[] ReadyStates = {"ReadyRole"};
        private static string[] DeletingStates ={"StoppingRole","StoppingVM","DeletingVM","StoppedVM","StoppedDeallocated"};
        private static string[] ProblemStates = {"RestartingRole", "CyclingRole", "FailedStartingRole", "FailedStartingVM", "UnresponsiveRole"};

        public string AzureStateName {get;set;}
        public string AzureStateDetails {get;set;}
      
        public InstanceStateSummary State {
            get{
            
                if(AzureStateName == null) return InstanceStateSummary.Unknown;

                if(PreparingStates.Contains(AzureStateName)) { return InstanceStateSummary.Preparing;}             
                if(ReadyStates.Contains(AzureStateName)) { return InstanceStateSummary.Ready;}
                if(DeletingStates.Contains(AzureStateName)) { return InstanceStateSummary.Deleting;}
                if(ProblemStates.Contains(AzureStateName)) { return InstanceStateSummary.Problem;}
                    
                return InstanceStateSummary.Unknown;           
            }
       }
        
    }

    public class ClusterState
    {
        public List<InstanceState> InstanceStates;

        public bool ClusterReady
        {
            get {

                if (InstanceStates != null && InstanceStates.Any())
                {
                    
                    return InstanceStates.All(x => x.State == InstanceState.InstanceStateSummary.Ready);
                }

                return false;
            
            }
        }
    }
}
