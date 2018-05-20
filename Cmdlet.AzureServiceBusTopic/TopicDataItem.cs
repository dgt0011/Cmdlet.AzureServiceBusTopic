using System;

namespace Cmdlet.AzureServiceBus
{
    internal class TopicDataItem
    {
        public string Path { get; set; }
        public TimeSpan  DefaultMessageTimeToLive { get; set; }
        public TimeSpan AutoDeleteOnIdle { get; set; }
        public long MaxSizeInMegabytes { get; set; }
        public bool RequiresDuplicateDetection { get; set; }
        public TimeSpan  DuplicateDetectionHistoryTimeWindow { get; set; }
        public bool EnableBatchedOperations  { get; set; }
        public bool SupportOrdering  { get; set; }
        public bool EnableFilteringMessagesBeforePublishing { get; set; }
        public bool IsAnonymousAccessible { get; set; }
        public bool EnablePartitioning { get; set; }
        public bool EnableExpress { get; set; }
}
}
