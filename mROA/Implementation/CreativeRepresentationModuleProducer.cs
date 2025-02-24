using System;
using mROA.Abstract;

namespace mROA.Implementation
{
    public class CreativeRepresentationModuleProducer : IRepresentationModuleProducer
    {
        private Type _reprModuleType;
        private IInjectableModule[] _creationModules;
        private IConnectionHub? _hub;

        public CreativeRepresentationModuleProducer(IInjectableModule[] creationModules, Type reprModuleType)
        {
            _creationModules = creationModules;
            _reprModuleType = reprModuleType;
        }


        public void Inject<T>(T dependency)
        {
            if (dependency is IConnectionHub interactionModule)
                _hub = interactionModule;
        }

        public IRepresentationModule Produce(int id)
        {
            if (_hub == null)
                throw new NullReferenceException("Interaction module is null");

            var produced =
                Activator.CreateInstance(_reprModuleType) as IRepresentationModule ??
                throw new Exception("Bad serialization module type");

            foreach (var creationModule in _creationModules)
                produced.Inject(creationModule);

            var interaction = _hub.GetInteracion(id);
            produced.Inject(interaction);
        
            return produced;
        }
    }
}