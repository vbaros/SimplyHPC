What is SimplyHPC ?
============

SimplyHPC is simply ... a unified framework to submit an application directly to the cloud in a simple way. 
The platform integrates Azure specific HPC libraries with PowerShell commandlets to ease the submission process while keeping 
the desired command line environment. The framework provides tools with the ability to deploy necessary number of virtual machines dynamically, 
submit the packed application together with input and configuration files, execute the simulation and, once the results are ready, 
download them to the local machine and stop the virtual machines. 

At the moment, the following platforms are supported:

- Microsoft Windows-based OS 32-bit/64-bit

SimplyHPC is intended to be the most painless, easy, quick and convenient way
to submit HPC jobs to the Microsoft Azure, execute them and download results. 
All from the command line.

Installation
-----

1. Checkout the github repo.
2. Rename <repo>\SimplyHPC\Controller\Properties\Resources_empty.resx to Resources.resx
and add your password to access VMs.
Password is defined in Azure Management Portal.
3. Load the Solution into Microsoft .NET
4. Right-click on Solution in Solution Explorer and Enable NuGet Restore.
5. Build the Solution.
6. Install the Module by executing in PowerShell following commands:
  
  `$env:PSModulePath`
  
 to get the list of locations where modules are installed. 

  Copy the content of <repo>\SimplyHPC\HSR.AzureEE.Module\ to a directory where PowerShell is storing the modules.
  
  `Import-Module HSR.AzureEE.Module`

  `Get-Module HSR.AzureEE.Module`
  
7. Create Management Certificate
8. Edit AzureConfigActive.xml and add the configuration information.
9. Test if the command lets work by executing:

`$params = New-AzureParameters AzureConfigActive`

`Get-AvailableRoleSizes $params`

You should see a list of available Roles.
  
Usage
-----
Once the SimplyHPC is installed you have access to a selection of commandlets:

* `NewAzureService` Create a new cloud service and the cluster 
* `NewAzureJob` Create and execute a new job 
* `NewAzureParameters` Create a set of parameters required by other commandlets    
* `GetJobStatus` Get the status of a given job    
* `GetJobResults` Get the results of a given job  
* `GetAvailableRoleSizes` Get Available VMs with their roles and sizes 
* `RemoveAzureJobs` Remove Azure Jobs such as unfinished jobs 
* `RemoveAzureService` Remove the cloud service and destroy the cluster 

These commandlets can be also combined into a single script. 

Open Source
-----------
We are dedicated to open source. Open source allow other
developers to port the application to new platforms that the original
authors did not begin to think of and it permits others to use the program in totally new ways, and enhance it in all imaginable ways.

Therefore, SimplyHPC is licensed under the Apache License. Exceptions are listed in the
[LICENSES](https://github.com/vbaros/SimplyHPC/blob/master/LICENSE) file.

Participating
-------------

[Pull Requests](https://help.github.com/articles/using-pull-requests)
are very welcome!

Authors
-------

SimplyHPC was created at [Microsoft Innovation Center Rapperswil](www.msic.ch) and is maintained by Vladimir Baros, SimplyHPC 0.x
was created by Michael Plecic.  The project is also supported by Lukasz Miroslaw.
