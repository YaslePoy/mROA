using System;
using System.Collections.Generic;
using mROA.Abstract;

namespace mROA.Implementation.Backend
{
    public class MultiClientContextRepository : IContextRepository, IContextRepositoryHub
    {
        private Dictionary<int, IContextRepository> _repositories = new();
        private readonly Func<int, IContextRepository> _produceRepository;

        public MultiClientContextRepository(Func<int, IContextRepository> produceRepository)
        {
            _produceRepository = produceRepository;
        }

        private IContextRepository GetRepositoryByClientId(int clientId)
        {
            if (_repositories.TryGetValue(clientId, out var repository))
                return repository;
        
            var created = _produceRepository(clientId);
            _repositories.Add(clientId, created);
            return created;
        }
        public void Inject<T>(T dependency)
        {
        }

        public int ResisterObject(object o)
        {
            var repository = GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId());
            return repository.ResisterObject(o);
        }

        public void ClearObject(int id)
        {
            var repository = GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId());
            repository.ClearObject(id);
        }

        public T GetObjectBySharedObject<T>(SharedObjectShellShell<T> sharedObjectShellShell)
        {
            var repository = GetRepository(sharedObjectShellShell.Identifier.OwnerId);
            return repository.GetObject<T>(sharedObjectShellShell.Identifier.ContextId);
        }

        public object GetObject(int id)
        {
            var repository = GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId());
            return repository.GetObject<object>(id);
        }

        public T? GetObject<T>(int id)
        {
            var repository = GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId());
            return repository.GetObject<T>(id);
        }

        public object GetSingleObject(Type type)
        {
            var repository = GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId());
            return repository.GetSingleObject(type);
        }

        public int GetObjectIndex(object o)
        {
            var repository = GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId());
            return repository.GetObjectIndex(o);
        }

        public IContextRepository GetRepository(int clientId)
        {
            var repository = GetRepositoryByClientId(clientId);
            return repository;
        }
    }
}