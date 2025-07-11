using System;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class CreativeRepresentationModuleProducer : IRepresentationModuleProducer
    {
        private IServiceProvider _creationModules;
        private IConnectionHub _hub;
        private IContextualSerializationToolKit _serialization;
        public CreativeRepresentationModuleProducer(IServiceProvider creationModules, IConnectionHub hub, IContextualSerializationToolKit serialization)
        {
            _creationModules = creationModules;
            _hub = hub;
            _serialization = serialization;
        }


        public IRepresentationModule Produce(int id)
        {
            if (_hub == null)
                throw new NullReferenceException("Interaction module is null");

            var interaction = _hub.GetInteraction(id);
            
            var produced = new RepresentationModule(interaction, _serialization);

            return produced;
        }
    }
}