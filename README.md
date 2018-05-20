# Cmdlet.AzureServiceBusTopic
A simple Powershell Cmdlet for some simple Azure ServiceBus Topic administration tasks/actions.

## Version 0.1

Consider this version alpha :)

## Requirements

Requires Visual Studio 2017 to build.

Requires PowerShell 3.0 or above to execute.  An Azure subscription with a ServiceBus or two would also be helpful.

## Usage

Once the project has been compiled, the assembly module will need to be imported into Powershell via Import-Module (change the path as appropriate).  

`Import-Module -Name C:\working_repos\Cmdlet.AzureServiceBus.dll -Verbose`

Once you've done this, all the cmdlets will be at your disposal, you can see a full list using `Get-Command -Module Cmdlet.AzureServiceBus`

`Get-AzureServiceBusNamespaceManager` is used to get an instance of the Microsoft.ServiceBus.NamespaceManager to provide access to your Azure ServiceBus instance.  You need to pass three parameters, the Namespace for your ServiceBus, a SharedAccessKeyName and a SharedAccessKey.  You can obtain all three values from the Primary Connection String (or Secondary) for the Shared Access Policy for your ServiceBus (access this by opening the Shared access policies blade under your ServiceBus in the Azure management portal)

e.g.

`Endpoint=sb://{namepace}.servicebus.windows.net/;SharedAccessKeyName={shared access key name};SharedAccessKey={shared access key}`

Once you have an instance of the NamespaceManager, this can then be passed to any of the other cmdlets via the pipeline.

### Example - Using Copy-AzureServiceBusTopicsToFile

`Copy-AzureServiceBusTopicsToFile` will read all topics for the ServiceBus & write the properties for each topic into a JSON serialised file.  Very useful for copying 100+ topics from one ServiceBus to another for testing purposes or backing up before using other cmdlets.

Two parameters must be provided;

A destination file must be provided.  If the file already exists, it will be overwritten.

A (topic) Path must be provided.  Wildcards are supported, so if you want all topics to be saved, use * 

`Get-AzureServiceBusNamespaceManager dgt0011-sb MySharedAccessKey H4rAV4OL337x7Q9Qn+4jX4/`
`KomJrBX0EXZAns= | Copy-AzureServiceBusTopicsToFile c:\temp\sb-topics-backup.json *.a -Verbose`

### Example - Using New-AzureServiceBusTopicsFromFile

`New-AzureServiceBusTopicsFromFile` will read that nifty JSON serialised list of topics saved earlier and will (re)create the topics on either a new ServiceBus or on your current ServiceBus if you've abused `Remove-AzureServiceBusTopic`

Two parameters must be provided;

A source file must be provided.  Obviously.

A (topic) Path must be provided.  Wildcards are supported, so if you want all topics to be created from the saved file, use *.  Note: If you've saved all topics into the file but only want to load a selection, you can limit the topics recreated with the wildcard feature.  E.g. to only load the topics that have a name starting with t.company. (and leave t.logging behind) use the following;

 `Get-AzureServiceBusNamespaceManager dgt0011-sb MySharedAccessKey H4rAV4OL337x7Q9Qn+4jX4/`
`KomJrBX0EXZAns= |New-AzureServiceBusTopicsFromFile c:\temp\sb-topics-backup.json t.company.* -Verbose`

### Example - Using Enable-AzureServiceBusTopicPartitioning

Have a production ServiceBus with 100+ topics & your support team tell you that you have to set partitioning on all topics?  `Enable-AzureServiceBusTopicPartitioning` is for you!

Each topic will be removed then re-created with the EnablePartitioning property enabled.  No need to manually delete & recreate or script each individually.

**Topics that already have EnablePartitioning are skipped.**

One parameter must be provided;

A (topic) Path must be provided.  Wildcards are supported.  If you only want a subset of you topics to be removed/recreated to enable partitioning, pass an appropriate wildcard (Or * for all topics)

Optionally you can also pass `-DebugFlag` to run without actually doing the remove & recreate so you can gauge the effect before running against that production bus.*

To test enable partitioning on all topics ending with .a;

`Get-AzureServiceBusNamespaceManager dgt0011-sb MySharedAccessKey H4rAV4OL337x7Q9Qn+4jX4/KomJrBX0EXZAns= | Enable-AzureServiceBusTopicPartitioning *.a -DebugFlag -Verbose`



*Probably a good idea to have a copy of a topic file from Copy-AzureServiceBusTopicsToFile as a roll back

## Scope & Contributing

This module has been created to suit my teams immediate requirements. Its also my first attempt at a Powershell cmdlet, so please be gentle.

Contributions are gratefully received however, so please feel free to submit a pull request with additional features or amendments.

## Author

Author:: Darren Tuer ([darren.tuer@gmail.com](mailto:darren.tuer@gmail.com))