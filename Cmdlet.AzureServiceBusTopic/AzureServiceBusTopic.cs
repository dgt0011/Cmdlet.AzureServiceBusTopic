namespace Cmdlet.AzureServiceBus
{
    using AutoMapper;
    using Microsoft.ServiceBus.Messaging;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Management.Automation;

    [Cmdlet(VerbsCommon.Get, "AzureServiceBusTopics")]
    public class AzureServiceBusTopics : AzureServiceBusBaseCmdlet
    {
        protected override void ProcessRecord()
        {
            var topics = NamespaceManager.GetTopics();
            if (Path != null)
            {
                topics = topics.Where(a => Path.Contains(a.Path));
            }

            WriteObject(topics, true);
        }
    }

    [Cmdlet(VerbsCommon.Remove, "AzureServiceBusTopic")]
    public class RemoveAzureServiceBusTopic : AzureServiceBusBaseCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The Name of the topic to remove")]
        public new string[] Path { get; set; }

        private bool _debug;

        [Parameter(Mandatory = false, Position = 1, HelpMessage = "Setting DebugMode to true(1) will only log the topic to delete but will not delete.")]
        public SwitchParameter DebugFlag
        {
            get => _debug;
            set => _debug = value;
        }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            foreach (var path in Path)
            {
                WriteVerbose($"Attempting to delete Topic '{path}' ...");

                if (NamespaceManager.TopicExists(path))
                {
                    WriteVerbose($"Deleting Topic '{path}' ...");

                    if (DebugFlag.IsPresent)
                    {
                        //All debug messages will appear with the -Debug option.
                        WriteVerbose($"Debug mode enabled - no action taken.");
                    }
                    else
                    {
                        NamespaceManager.DeleteTopic(path);
                        if (NamespaceManager.TopicExists(path))
                        {
                            WriteError(new ErrorRecord(new ApplicationException($"Failed to delete Topic {path}"), "", ErrorCategory.ResourceExists,null));
                        }
                        else
                        {
                            WriteVerbose($"Topic '{path}' deleted.");

                        }
                    }
                }
                else
                {
                    WriteWarning($"Topic {path} does not exist.");                   
                }
            }
        }
    }

    [Cmdlet(VerbsCommon.New, "AzureServiceBusTopic")]
    public class CreateAzureServiceBusTopic : AzureServiceBusBaseCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The Name of the topic")]
        public new string[] Path { get; set; }

        [Parameter(HelpMessage = "The maximum size in increments of 1024MB for the queue, maximum of 5120MB")]
        public long? MaxSize { get; set; }

        [Parameter(HelpMessage = "The default time-to-live to apply to all messages, in seconds")]
        public long? DefaultMessageTimeToLive { get; set; }

        private bool _enablePartitioning = true;

        [Parameter(HelpMessage = "Partitions a topic across multiple message brokers and message stores.  Disconnects the overall throughput of a partitioned entity from any single message broker or messaging store.")]
        public SwitchParameter EnablePartitioning
        {
            get => _enablePartitioning;
            set => _enablePartitioning = value;
        }

        private bool _enableDuplicateDetection;

        [Parameter(HelpMessage = "Enabling duplicate detection configures your topic to keep a history of all messages sent to the topic for a configurable amount of time.  During that time the topic will not sore any duplicate messages.")]
        public SwitchParameter EnableDuplicateDetection
        {
            get => _enableDuplicateDetection;
            set => _enableDuplicateDetection = value;
        }

        [Parameter(HelpMessage = "The duplicate detection timeframe, in seconds")]
        public long? DuplicateDetectionTimeInSeconds { get; set; }
        
        protected override void ProcessRecord()
        {
            base.ProcessRecord();
            foreach (var path in Path)
            {
                if (NamespaceManager.TopicExists(path))
                {
                    WriteWarning($"Topic {path} already exists and will not be re-created.");
                }
                else
                {
                    TopicDescription newTopic = new TopicDescription(path);
                    if (MaxSize.HasValue)
                    {
                        newTopic.MaxSizeInMegabytes = MaxSize.Value;
                    }
                    
                    int value = 0;
                    if (DefaultMessageTimeToLive.HasValue)
                    {
                        int.TryParse(DefaultMessageTimeToLive.Value.ToString(), out value);
                    }

                    if (value == 0)
                    {
                        value = 1209600;//the 14 days that is the default in the UI
                    }

                    newTopic.DefaultMessageTimeToLive = new TimeSpan(0, 0, 0, value);

                    value = 0;
                    if (EnableDuplicateDetection.IsPresent)
                    {
                        newTopic.RequiresDuplicateDetection = true;
                        if (DuplicateDetectionTimeInSeconds.HasValue)
                        {
                            int.TryParse(DuplicateDetectionTimeInSeconds.Value.ToString(), out value);
                        }
                        if(value == 0)
                        {
                            value = 30; //the default in the UI
                        }

                        newTopic.DuplicateDetectionHistoryTimeWindow = new TimeSpan(0, 0, 0, value);
                    }

                    newTopic.EnablePartitioning = EnablePartitioning.IsPresent;

                    NamespaceManager.CreateTopic(newTopic);

                    if (NamespaceManager.TopicExists(path))
                    {
                        WriteProgress(new ProgressRecord(0, "CreateTopic", $"Topic {path} created."));
                    }
                    else
                    {
                        WriteError(new ErrorRecord(new ApplicationException($"Failed to create Topic {path}"), "", ErrorCategory.ResourceUnavailable, null));
                    }
                }
            }
        }
    }

    [Cmdlet("Enable", "AzureServiceBusTopicPartitioning")]
    public class EnableAzureServiceBusTopicPartitioning : AzureServiceBusBaseCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The name of one or more topics to set the Enable Partitioning flag to true. Wildcards are permitted.")]
        public new string[] Path { get; set; }


        private bool _debug;

        [Parameter(Mandatory = false, Position = 1, HelpMessage = "Setting DebugFlag will only log the topic to delete and recreate with Enable Partitioning set to True but will not make changes.")]
        public SwitchParameter DebugFlag
        {
            get => _debug;
            set => _debug = value;
        }

        protected override void ProcessRecord()
        {
            base.ProcessRecord();

            var topics = NamespaceManager.GetTopics().ToList();

            foreach (var path in Path)
            {
                // Write a user-friendly verbose message to the pipeline. These messages will appear with the -Verbose option.
                var message = $"Attempting to remove and recreate topic/s named \"{path}\" with Enable Partitioning set.";
                WriteVerbose(message);

                // Validate the path name against a wildcard pattern.  
                // If the name does not contain any wildcard patterns, it 
                // will be treated as an exact match.
                WildcardOptions options = WildcardOptions.IgnoreCase | WildcardOptions.Compiled;
                WildcardPattern wildcard = new WildcardPattern(path, options);

                foreach (var topic in topics)
                {
                    if (!wildcard.IsMatch(topic.Path))
                    {
                        WriteVerbose($"Topic '{topic.Path}' did not match '{path}' - skipping.");
                        continue;
                    }

                    if (topic.EnablePartitioning)
                    {
                        WriteVerbose($"Topic '{topic.Path}' already has Enable Partitioning set - skipping.");
                        continue;
                    }

                    WriteVerbose($"Removing Topic '{topic.Path}' ...");

                    if (DebugFlag.IsPresent)
                    {
                        WriteVerbose($"Debug flag is set - no action taken");
                    }
                    else
                    {
                        NamespaceManager.DeleteTopic(topic.Path);

                        if (NamespaceManager.TopicExists(topic.Path))
                        {
                            WriteError(new ErrorRecord(new ApplicationException($"Failed to delete Topic {path}"), "", ErrorCategory.ResourceExists, null));
                        }
                        else
                        {
                            WriteVerbose($"Topic '{topic.Path}' removed.");
                        }
                    }

                    WriteVerbose($"Recreating Topic '{topic.Path}' ...");

                    var newTopic =
                        new TopicDescription(topic.Path)
                        {
                            DefaultMessageTimeToLive = topic.DefaultMessageTimeToLive,
                            AutoDeleteOnIdle = topic.AutoDeleteOnIdle,
                            MaxSizeInMegabytes = topic.MaxSizeInMegabytes,
                            RequiresDuplicateDetection = topic.RequiresDuplicateDetection,
                            DuplicateDetectionHistoryTimeWindow = topic.DuplicateDetectionHistoryTimeWindow,
                            EnableBatchedOperations = topic.EnableBatchedOperations,
                            SupportOrdering = topic.SupportOrdering,
                            EnableFilteringMessagesBeforePublishing = topic.EnableFilteringMessagesBeforePublishing,
                            IsAnonymousAccessible = topic.IsAnonymousAccessible,
                            EnablePartitioning = true,
                            EnableExpress = topic.EnableExpress
                        };

                    if (DebugFlag.IsPresent)
                    {
                        WriteVerbose($"Debug flag is set - no action taken");
                    }
                    else
                    {
                        NamespaceManager.CreateTopic(newTopic);

                        if (NamespaceManager.TopicExists(topic.Path))
                        {
                            WriteVerbose($"Topic '{topic.Path}' recreated.");
                        }
                        else
                        {
                            WriteError(new ErrorRecord(new ApplicationException($"Failed to create Topic {topic.Path}"), "", ErrorCategory.ResourceUnavailable, null));
                        }                        
                    }
                }
            }
        }
    }

    [Cmdlet(VerbsCommon.Copy, "AzureServiceBusTopicsToFile")]
    public class CopyAzureServiceBusTopicsToFile : AzureServiceBusBaseCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The full path of the file to write a copy of Topics to.")]
        public string FilePath { get; set; }

        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The name of one or more topics to write to file. Wildcards are permitted.")]
        public new string[] Path { get; set; }

        public CopyAzureServiceBusTopicsToFile()
        {
            Bootstrapper.Instance.InitialiseAutomapper();
        }

        protected override void ProcessRecord()
        {
            var topics = NamespaceManager.GetTopics().ToList();
            var topicFileItems = new List<TopicDataItem>();

            foreach (var path in Path)
            {
                // Write a user-friendly verbose message to the pipeline. These messages will appear with the -Verbose option.
                var message = $"Attempting to save topic/s named \"{path}\" to file{FilePath}.";
                WriteVerbose(message);

                // Validate the path name against a wildcard pattern.  
                // If the name does not contain any wildcard patterns, it 
                // will be treated as an exact match.
                WildcardOptions options = WildcardOptions.IgnoreCase | WildcardOptions.Compiled;
                WildcardPattern wildcard = new WildcardPattern(path, options);

                foreach (var topic in topics)
                {
                    if (!wildcard.IsMatch(topic.Path))
                    {
                        WriteVerbose($"Topic '{topic.Path}' did not match '{path}' - skipping.");
                        continue;
                    }

                    WriteVerbose($"Reading Topic '{topic.Path}' ...");

                    var topicItem = new TopicDataItem();
                    Mapper.Map(topic, topicItem);
                    topicFileItems.Add(topicItem);
                }
            }

            FileInfo file = new FileInfo(FilePath);
            if (file.Exists)
            {
                WriteWarning($"File '{FilePath} already exists & will be overwritten.");
            }

            if (file.Directory != null && !file.Directory.Exists)
            {
                WriteError(new ErrorRecord(new ApplicationException($"Path '{file.Directory.Name}' does not exist."), "", ErrorCategory.InvalidArgument, null));
                return;
            }

            File.WriteAllText(FilePath, JsonConvert.SerializeObject(topicFileItems));

            WriteVerbose($"{topicFileItems.Count} Topics written to '{FilePath}'.");
        }
    }

    [Cmdlet(VerbsCommon.New, "AzureServiceBusTopicsFromFile")]
    public class CreateAzureServiceBusTopicsFromFile : AzureServiceBusBaseCmdlet
    {
        [Parameter(Mandatory = true, Position = 0, HelpMessage = "The full path of the file to write a copy of Topics to.")]
        public string FilePath { get; set; }

        [Parameter(Mandatory = true, Position = 1, HelpMessage = "The name of one or more topics to write to file. Wildcards are permitted.")]
        public new string[] Path { get; set; }

        private bool _debug;

        [Parameter(Mandatory = false, Position = 2, HelpMessage = "Setting DebugFlag will only log the topic to create but will not make changes to the service bus.")]
        public SwitchParameter DebugFlag
        {
            get => _debug;
            set => _debug = value;
        }

        public CreateAzureServiceBusTopicsFromFile()
        {
            Bootstrapper.Instance.InitialiseAutomapper();
        }

        protected override void ProcessRecord()
        {
            List<TopicDataItem> topicItems;

            var fileInfo = new FileInfo(FilePath);
            if (!fileInfo.Exists)
            {
                WriteError(new ErrorRecord(new ApplicationException($"File '{FilePath}' does not exist."), "", ErrorCategory.InvalidArgument, null));
                return;
            }

            try
            {
                topicItems = JsonConvert.DeserializeObject<List<TopicDataItem>>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                WriteError(new ErrorRecord(e, "", ErrorCategory.OpenError, null));
                return;
            }

            foreach (var path in Path)
            {
                var message = $"Attempting to create topic/s named \"{path}\" from file '{FilePath}'.";
                WriteVerbose(message);

                WildcardOptions options = WildcardOptions.IgnoreCase | WildcardOptions.Compiled;
                WildcardPattern wildcard = new WildcardPattern(path, options);

                foreach (var topicItem in topicItems)
                {
                    if (!wildcard.IsMatch(topicItem.Path))
                    {
                        WriteVerbose($"File copy of Topic '{topicItem.Path}' did not match '{path}' - skipping.");
                        continue;
                    }

                    var newTopic = new TopicDescription(topicItem.Path);
                    Mapper.Map(topicItem, newTopic);

                    if (DebugFlag.IsPresent)
                    {
                        WriteVerbose($"Debug flag is set - no action taken");
                        continue;
                    }

                    NamespaceManager.CreateTopic(newTopic);

                    if (NamespaceManager.TopicExists(newTopic.Path))
                    {
                        WriteProgress(new ProgressRecord(0, "CreateTopic", $"Topic {newTopic.Path} created."));
                    }
                    else
                    {
                        WriteError(new ErrorRecord(new ApplicationException($"Failed to create Topic {newTopic.Path}"), "", ErrorCategory.ResourceUnavailable, null));
                    }

                }
            }
        }
    }
}

