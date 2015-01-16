#  Version 1.00
#  Script name:     SubmitAzureJob.ps1 
#  Created on:      16.06.2014.
#  Author:          Vladimir Baros
#  Purpose:         Submits an MPI job to Azure

#  Description:		Run MPI job on Azure checklist:
#                   - verify that executable to submit and all the dependencies (dlls, data...) reside in the same folder
#                   - run the MPI executable ones or better check with DependencyWalker tool if all dlls are present
#                     in the same folder (www.dependencywalker.com)
#                   - on Azure create storage folder and affinity group you would like to use
#                   - create a management certificate and save it as in .pfx format. Remember the password
#                   - encrypt the password using included SaveCredentials.ps1.
#                     NOTICE: Encrypted password will work only with your computer and user account!!!
#                   - populate the AzureConfiguration.xml with Azure data from your account
#                   - set all script configuration parameters, $ClusterName needs to be unique

# import HSR.AzureEE.Module
Import-Module HSR.AzureEE.Module -ea SilentlyContinue

if ( (Get-Module HSR.AzureEE.Module) -eq $null ) {
    Write-Error "HSR.AzureEE.Module can't be found at the following locations:`r`n $env:PSModulePath"
    return
}

# configuration, cluster
$XMLConfigurationPath	  = 'AzureConfiguration.xml'
$pathToDeploymentTemplate = ''

# configuration, job
$clusterName		= '' # needs to be unique. Can contain only letters, numbers, and hyphens.
                         # The first and last character in the field must be a letter or number.
                         # Trademarks, reserved words, and offensive words are not allowed.
$sizeOfNodes        = '' # Small, Large...
$numberOfNodes      = 1
$jobDataDirectory   = ''
$jobExecutable      = ''
$jobParameters      = ''

$maximumjobexecutiontime  = '00:00:00' # set maximum job execution time, zero means unlimited

#region functions
function ElapsedTime ($sw) {
    $sw.Stop()
    [console]::WriteLine("Elapsed time (create cluster + job execution) is: {0:c}", $sw.Elapsed)
}

function ParametersOK ($clusterName) {
    $formatOK = ($clusterName -match '^[a-z0-9](?:[a-z0-9]|(\-(?!\-))){1,61}[a-z0-9]$')
    
    if (-Not ($formatOK)) {
        Write-Host ("`'$clusterName`' is not a valid name for an Azure service or container.")
    }

    return $formatOK
}

#endregion

# verify parameters
if (-Not (ParametersOK ($clusterName))) {
    return
}

# start stopwatch
$sw = [Diagnostics.Stopwatch]::StartNew()

# create cluster
$azureservice = New-AzureService $XMLConfigurationPath $clusterName $sizeOfNodes $numberOfNodes $pathToDeploymentTemplate

# submit job
if ($azureservice) {
    Write-Host ("Uploading job data and running...")
    $jobID = New-AzureJob $azureservice $clusterName $jobDataDirectory $jobExecutable $jobParameters
}

# check job status every 5 seconds and download results if the job is finished
$jobmaxtimespan = ([TimeSpan]::Parse($maximumjobexecutiontime))
if ($jobmaxtimespan -gt 0) {
    $jobstopwatch = [Diagnostics.Stopwatch]::StartNew()
}

if ($jobID) {
    while ($true) {
        sleep 5
        $jobfinished = Get-JobStatus $azureservice $clusterName $jobID
        
        if ($jobfinished) {
            # download result in the current directory
            $downloaded = Get-JobResults $azureservice $clusterName $jobID
            
            if ($downloaded) {
                Write-Host ("Job results downloaded.")
            }
            else {
                Write-Error ("Job results were not downloaded.")
            }

            break
        }
        
        if (($jobmaxtimespan -gt 0) -and ($jobstopwatch.Elapsed -gt $jobmaxtimespan)) {
            Write-Host ("Job execution was longer than predefined time of $maximumjobexecutiontime (hh:mm:ss).")
            Write-Host ("Job results were not downloaded.")
            
            $jobstopwatch.Stop()
            break
        }
    }
}

# remove azure service and the deployment
if ($azureservice) {
    Write-Host ("Deleting Azure service and the complete deployment...")
    Remove-AzureService $azureservice $clusterName
}

# end stopwatch and write elapsed time
ElapsedTime ($sw)