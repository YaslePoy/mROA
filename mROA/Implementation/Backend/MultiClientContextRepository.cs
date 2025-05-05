using System;
using System.Collections.Generic;
using mROA.Abstract;

namespace mROA.Implementation.Backend
{
    public class MultiClientContextRepository : IContextRepository, IContextRepositoryHub
    {
        private readonly Func<int, IContextRepository> _produceRepository;
        private readonly Dictionary<int, IContextRepository> _repositories = new();

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

        public T GetObject<T>(ComplexObjectIdentifier id, IEndPointContext context)
        {
            var repository = GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId());
            return repository.GetObject<T>(id, context);
        }

        public object GetSingleObject(Type type, int ownerId)
        {
            var repository = GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId());
            return repository.GetSingleObject(type, ownerId);
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

        public void FreeRepository(int clientId)
        {
            _repositories.Remove(clientId);
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