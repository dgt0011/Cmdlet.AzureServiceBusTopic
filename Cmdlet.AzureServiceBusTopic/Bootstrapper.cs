using AutoMapper;
using Microsoft.ServiceBus.Messaging;

namespace Cmdlet.AzureServiceBus
{
    using System;

    public sealed class Bootstrapper
    {
        private static readonly Lazy<Bootstrapper> Lazy = new Lazy<Bootstrapper>(() => new Bootstrapper());
        private bool _automapperInitialised;


        public static Bootstrapper Instance => Lazy.Value;

        private Bootstrapper()
        {
        }

        public void InitialiseAutomapper()
        {
            //we could also have done this in the constructor, but then the call would look somewhat odd in the caller
            // basically asking for a reference that we didnt do anything with
            if(!_automapperInitialised)
            {
                _automapperInitialised = true;
                Mapper.Initialize(cfg =>
                {
                    cfg.CreateMap<TopicDataItem, TopicDescription>();
                    // hacky work around for initialization only being allowed once per app domain - until I can shift into a singleton
                    cfg.CreateMap<TopicDescription, TopicDataItem>();
                });
            }
        }


    }
}
