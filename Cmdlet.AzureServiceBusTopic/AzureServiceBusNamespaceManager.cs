namespace Cmdlet.AzureServiceBus
{
    using Microsoft.ServiceBus;
    using System.Management.Automation;

    [Cmdlet(VerbsCommon.Get, "AzureServiceBusNamespaceManager")]
    public class AzureServiceBusNamespaceManager : System.Management.Automation.Cmdlet
    {
        [Parameter(Mandatory = true, Position = 0)]
        public string Namespace { get; set; }

        [Parameter(Mandatory = true, Position = 1)]
        public string SharedAccessKeyName { get; set; }

        [Parameter(Mandatory = true, Position = 2)]
        public string SharedAccessKey { get; set; }

        protected override void EndProcessing()
        {
            var connectString = $"Endpoint=sb://{Namespace}.servicebus.windows.net/;SharedAccessKeyName={SharedAccessKeyName};SharedAccessKey={SharedAccessKey}";
            WriteObject(NamespaceManager.CreateFromConnectionString(connectString));
        }
    }
}
