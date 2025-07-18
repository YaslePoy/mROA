using mROA.Abstract;

namespace mROA.Implementation
{
    public class CreativeRepresentationModuleProducer : IRepresentationModuleProducer
    {
        private readonly IConnectionHub _hub;
        private readonly IContextualSerializationToolKit _serialization;
        public CreativeRepresentationModuleProducer(IConnectionHub hub, IContextualSerializationToolKit serialization)
        {
            _hub = hub;
            _serialization = serialization;
        }


        public IRepresentationModule Produce(int id)
        {
            var interaction = _hub.GetInteraction(id);
            
            var produced = new RepresentationModule(interaction, _serialization);

            return produced;
        }
    }
}