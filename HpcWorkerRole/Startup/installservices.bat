
rem    Date:    06.03.2014 / 12.03.2014
rem    Author:  Vladimir Baros / MichaelPantic
rem	   Purpose: Create MPI User
rem				Install smpd.exe as a service and starts it
rem             Service has to run as the same user running MPI applications
rem				Copies & Installs mpid service and starts it
set username=MpiUser
set password=Mpi123!


rem Create user
net user /add "%username%" "%password%"
net localgroup administrators "%username%" /add


rem Install smpd service
call Startup\nssm install "SMPD service" "%programfiles%\Microsoft MPI\Bin\smpd.exe" "-p 8677"
Startup\nssm set "SMPD service" ObjectName "%COMPUTERNAME%\%username%" "%password%"
sc failure "SMPD service" actions= restart/60000/restart/60000/restart/60000 reset= 240
Startup\nssm start "SMPD service"


Rem Install mpid service


mkdir "%programfiles%\MPID"
robocopy "%CD%"  "%programfiles%\MPID" /S
call Startup\nssm install "MPID service" "%programfiles%\MPID\HSR.AzureEE.MpiWrapper.exe"
Startup\nssm set "MPID service" ObjectName "%COMPUTERNAME%\%username%" "%password%"
sc failure "MPID service" actions= restart/60000/restart/60000/restart/60000 reset= 240
Startup\nssm start "MPID service"

@echo on
EXIT /B 0