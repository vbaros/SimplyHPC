using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HSR.AzureEE.MpiWrapper
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, IncludeExceptionDetailInFaults=true)]
    public class WCFMPIRunner : IMPIRunner
    {
        protected static Process currentProcess;
        protected static string currentExecutable;
        protected static string currentWorkingDir;
        protected static StringBuilder currentStandardOutput;
        protected static StringBuilder currentErrorOutput;


        protected object threadLock = new object();

        static string MPICommand = null;

        public WCFMPIRunner()
        {   
            if (MPICommand == null)
            {
                MPICommand = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft MPI\\Bin\\mpiexec.exe");
            }
        }

        public int RunApplication(string executable, string parameters, string[] hosts = null, int numCores = 1, int nHosts = 1)
        {
            lock (threadLock)
            {
                if (GetState() == RunnerState.Idle)
                {
                    try
                    {
                        if (hosts != null) // MPI job
                        {
                            //create a new process with the correct arguments
                            currentProcess = PrepareMPIExec(executable, parameters, hosts, numCores, nHosts);
                            currentExecutable = executable;
                            currentWorkingDir = GetRootedDirectory(executable);

                            //Redirect standard and error output to strings
                            currentStandardOutput = new StringBuilder();
                            currentErrorOutput = new StringBuilder();
                            currentProcess.Exited += currentProcess_Exited; //when the process exits, execute this function (currentProcess_Exited)
                            currentProcess.ErrorDataReceived += currentProcess_ErrorDataReceived; //for each line of error output, execute this function
                            currentProcess.OutputDataReceived += currentProcess_OutputDataReceived; //for each line of standard output, execute this function

                            //start process
                            currentProcess.Start();
                            currentProcess.BeginErrorReadLine();
                            currentProcess.BeginOutputReadLine();
                            return currentProcess.Id;
                      
                        }
                        else // nonMPI job
                        {
                            //create a new process with the correct arguments
                            currentProcess = PrepareExec(executable, parameters);
                            currentExecutable = executable;
                            currentWorkingDir = GetRootedDirectory(executable);

                            //Redirect standard and error output to strings
                            currentStandardOutput = new StringBuilder();
                            currentErrorOutput = new StringBuilder();
                            currentProcess.Exited += currentProcess_Exited; //when the process exits, execute this function (currentProcess_Exited)
                            currentProcess.ErrorDataReceived += currentProcess_ErrorDataReceived; //for each line of error output, execute this function
                            currentProcess.OutputDataReceived += currentProcess_OutputDataReceived; //for each line of standard output, execute this function

                            //start process
                            currentProcess.Start();
                            currentProcess.BeginErrorReadLine();
                            currentProcess.BeginOutputReadLine();
                            return currentProcess.Id;
                        }
                    }
                    catch
                    {
                        currentProcess = null;
                        throw;
                    }
                                      
                }
            }
            throw new Exception("Runner not idle");
        }

        void currentProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            currentStandardOutput.AppendLine(e.Data); //write the line of standard output to string "currentStandardOutput"
        }

        void currentProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            currentErrorOutput.AppendLine(e.Data);
        }

        /// <summary>
        /// Gets executed when the process exited. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void currentProcess_Exited(object sender, EventArgs e)
        {
            //processExited File etc
            WriteProcessInfoFiles(currentProcess);
        }

        

        public RunnerState GetState()
        {
            lock (threadLock)
            {
                if (currentProcess == null)
                {
                    return RunnerState.Idle;
                }
                else
                {
                    return currentProcess.HasExited ? RunnerState.Finished : RunnerState.Running;
                }
            }
        }

        public string GetResultFilePath()
        {
            lock (threadLock)
            {
                //get all files that have changed since the process started
                var files = Directory.GetFiles(currentWorkingDir);

                var changedFiles = files.Where(x => File.GetLastWriteTime(x) > currentProcess.StartTime);

                var zipFilePath = Path.Combine(currentWorkingDir, DateTime.Now.Ticks+"results.zip");
                using (var zipFile = new ZipFile(zipFilePath))
                {
                    zipFile.AddFiles(changedFiles, "");
                    zipFile.Save();
                }

                //clear process
                currentProcess.Dispose();
                currentProcess = null;
                return zipFilePath;
            }
        }

        /// <summary>
        /// * Writes process infos (Filename, arguments, etc) to "processinfo.txt"
        /// * Writes StandardOutput to StandardOutput.txt
        /// * Writes StandardError to StandardError.txt
        /// </summary>
        /// <param name="proc"></param>
        protected static void WriteProcessInfoFiles(Process proc)
        {
            if (!proc.HasExited)
            {
                throw new Exception("Process has not exited yet");
            }

            //do not wait for any more output
            proc.CancelErrorRead();
            proc.CancelOutputRead();
            
            //create a new file 
            var procInfoFile = new StringBuilder();
            procInfoFile.AppendFormat("Filename:\t{0}", proc.StartInfo.FileName); procInfoFile.AppendLine();
            procInfoFile.AppendFormat("Arguments:\t{0}", proc.StartInfo.Arguments); procInfoFile.AppendLine();
            procInfoFile.AppendFormat("WorkingDirectory:\t{0}", proc.StartInfo.WorkingDirectory); procInfoFile.AppendLine();
            procInfoFile.AppendFormat("StartTime:\t{0}", proc.StartTime); procInfoFile.AppendLine();
            procInfoFile.AppendFormat("EndTime:\t{0}", proc.ExitTime); procInfoFile.AppendLine();
            procInfoFile.AppendFormat("TotalProcTime:\t{0}", proc.TotalProcessorTime); procInfoFile.AppendLine();
            procInfoFile.AppendFormat("ExitCode:\t{0}", proc.ExitCode); procInfoFile.AppendLine();

            File.WriteAllText(Path.Combine(currentWorkingDir, "ProcessInfo.txt"), procInfoFile.ToString());
            File.WriteAllText(Path.Combine(currentWorkingDir, "StandardOutput.txt"), currentStandardOutput.ToString());
            File.WriteAllText(Path.Combine(currentWorkingDir, "StandardError.txt"), currentErrorOutput.ToString());
        }

        protected static string GetRootedDirectory(string filename)
        {
            return Path.GetFullPath(Path.GetDirectoryName(filename));
        }

        protected static Process PrepareMPIExec(string executable, string arguments, string[] hosts, int nCores, int nHosts)
        {
             //build command line args
            var mpiExecArgs = new StringBuilder();

          
            if (hosts != null && hosts.Any())
            {
                string[] runHosts;
                if (nHosts < hosts.Count())
                {
                    runHosts = hosts.Take(nHosts).ToArray();
                }
                else
                {
                    runHosts = hosts;
                }

                mpiExecArgs.AppendFormat(" -c {0} ", nCores);
                mpiExecArgs.AppendFormat(" -hosts {0} ", runHosts.Length);
                runHosts.ToList().ForEach(x =>
                {
                    mpiExecArgs.AppendFormat(" {0} ", x);
                });
            }
            else
            {
                mpiExecArgs.AppendFormat(" -n {0} ", nCores);
            }

            mpiExecArgs.Append(" \"" + Path.GetFileName(executable) + "\" ");

            mpiExecArgs.Append(" " +arguments+ " ");

            var mpiProcess = new Process();
            var workingDir = GetRootedDirectory(executable);


            mpiProcess.StartInfo = new ProcessStartInfo()
            {
                FileName = MPICommand,
                WorkingDirectory = workingDir,
                Arguments = mpiExecArgs.ToString(),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            // log

            //using (StreamWriter w = File.AppendText(@"e:\log.txt"))
            //{
            //    w.WriteLine (MPICommand);
            //    w.WriteLine (workingDir);
            //    w.WriteLine (mpiExecArgs.ToString());
            //}


            return mpiProcess;
        }
        protected static Process PrepareExec(string executable, string arguments)
        {
            var process = new Process();
            var workingDir = GetRootedDirectory(executable);

            process.StartInfo = new ProcessStartInfo()
            {
                FileName = Path.GetFileName(executable),
                WorkingDirectory = workingDir,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            // log

            //using (StreamWriter w = File.AppendText(@"e:\log.txt"))
            //{
            //    w.WriteLine (workingDir);
            //}

            return process;
        }


        public string GetCurrentStandardOutput()
        {
            return currentStandardOutput.ToString();
        }
    }
}
