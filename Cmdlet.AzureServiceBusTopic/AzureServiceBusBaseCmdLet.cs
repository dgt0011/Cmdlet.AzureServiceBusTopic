namespace Cmdlet.AzureServiceBus
{
    using Microsoft.ServiceBus;
    using System.Management.Automation;

    public abstract class AzureServiceBusBaseCmdlet : Cmdlet
    {
        [Parameter(Mandatory = false, Position = 0)]
        public string[] Path { get; set; }

        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        public NamespaceManager NamespaceManager { get; set; }
    }
}
