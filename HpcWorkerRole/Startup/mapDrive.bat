rem    Date:    06.03.2014 / 12.03.2014
rem    Author:  Lukasz Miroslaw
rem	   Purpose: Map a Storage from Windows Azure to a local drive on Azure VM
rem    Storage key can be found in your Azure Management Console -> Storages -> Manage Access Keys

set storageEndpoint=\\hsropenfoam.file.core.windows.net\ofazure
set key=qsCqwb/bdeqLzL9dc2nTTuKONeJVwzdhJbtnyGSQy5vbjx9C7lx0A88lHZjk6QCPHILghcfbw6GPTPPpJQ96rg==  


net use z: %storageEndpoint% /u:hsropenfoam %key%