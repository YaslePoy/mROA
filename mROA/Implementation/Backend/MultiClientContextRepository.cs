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
            return GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId()).ResisterObject(o);
        }

        public void ClearObject(int id)
        {
            GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId()).ClearObject(id);
        }

        public T GetObjectBySharedObject<T>(SharedObject<T> sharedObject)
        {
            return GetRepository(sharedObject.OwnerId).GetObject<T>(sharedObject.ContextId);
        }

        public object GetObject(int id)
        {
            return GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId()).GetObject<object>(id);
        }

        public T? GetObject<T>(int id)
        {
            return GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId()).GetObject<T>(id);
        }

        public object GetSingleObject(Type type)
        {
            return GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId()).GetSingleObject(type); 
        }

        public int GetObjectIndex(object o)
        {
            return GetRepositoryByClientId(TransmissionConfig.OwnershipRepository.GetOwnershipId()).GetObjectIndex(o); 
        }

        public IContextRepository GetRepository(int clientId)
        {
            return GetRepositoryByClientId(clientId);
        }
    }
}