using System;
using System.Collections.Generic;
using mROA.Abstract;

namespace mROA.Implementation.Backend
{
    public class MultiClientContextRepository : IContextRepository, IContextRepositoryHub
    {
        private readonly Func<int, IContextRepository> _produceRepository;
        private Dictionary<int, IContextRepository> _repositories = new();

        public MultiClientContextRepository(Func<int, IContextRepository> produceRepository)
        {
            _produceRepository = produceRepository;
        }

        public void Inject<T>(T dependency)
        {
        }

        public int HostId { get; set; }

        public int ResisterObject<T>(object o, IEndPointContext context)
        {
            var repository = GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId());
            return repository.ResisterObject<T>(o, context);
        }

        public void ClearObject(ComplexObjectIdentifier id)
        {
            var repository = GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId());
            repository.ClearObject(id);
        }

        public T GetObjectByShell<T>(SharedObjectShellShell<T> sharedObjectShellShell)
        {
            var repository = GetRepository(sharedObjectShellShell.Identifier.OwnerId);
            return repository.GetObject<T>(sharedObjectShellShell.Identifier)!;
        }

        public T? GetObject<T>(ComplexObjectIdentifier id)
        {
            var repository = GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId());
            return repository.GetObject<T>(id);
        }

        public object GetSingleObject(Type type)
        {
            var repository = GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId());
            return repository.GetSingleObject(type);
        }

        public int GetObjectIndex<T>(object o, IEndPointContext context)
        {
            var repository = GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId());
            return repository.GetObjectIndex<T>(o, context);
        }

        public IContextRepository GetRepository(int clientId)
        {
            var repository = GetRepositoryByClientId(clientId);
            return repository;
        }

        private IContextRepository GetRepositoryByClientId(int clientId)
        {
            if (_repositories.TryGetValue(clientId, out var repository))
                return repository;

            var created = _produceRepository(clientId);
            _repositories.Add(clientId, created);
            return created;
        }
    }
}