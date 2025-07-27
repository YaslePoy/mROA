using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using mROA.Abstract;

namespace mROA.Implementation.Backend
{
    public class MultiClientInstanceRepository : IRealStoreInstanceRepository, IContextRepositoryHub
    {
        private readonly Func<int, IInstanceRepository> _produceRepository;
        private readonly Dictionary<int, IInstanceRepository> _repositories = new();

        public MultiClientInstanceRepository(Func<int, IInstanceRepository> produceRepository)
        {
            _produceRepository = produceRepository;
        }

        public int ResisterObject<T>(object o, IEndPointContext context)
        {
            var repository = GetRepositoryByClientId(context.OwnerId);
            return repository.ResisterObject<T>(o, context);
        }

        public void ClearObject(ComplexObjectIdentifier id, IEndPointContext context)
        {
            var repository = GetRepositoryByClientId(context.OwnerId);
            repository.ClearObject(id, context);
        }

        public T GetObject<T>(ComplexObjectIdentifier id, IEndPointContext context) where T : class
        {
            var repository = GetRepositoryByClientId(context.OwnerId);
            return repository.GetObject<T>(id, context);
        }

        public T GetSingletonObject<T>(IEndPointContext context) where T : class, IShared
        {
            var repository = GetRepositoryByClientId(context.OwnerId);
            return repository.GetSingletonObject<T>(context);
        }

        public object GetSingletonObject(Type type, IEndPointContext context)
        {
            var repository = GetRepositoryByClientId(context.OwnerId);
            return repository.GetSingletonObject(type, context);
        }

        public int GetObjectIndex<T>(object o, IEndPointContext context)
        {
            var repository = GetRepositoryByClientId(context.OwnerId);
            return repository.GetObjectIndex<T>(o, context);
        }

        public IInstanceRepository GetRepository(int clientId)
        {
            var repository = GetRepositoryByClientId(clientId);
            return repository;
        }

        public void FreeRepository(int clientId)
        {
            _repositories.Remove(clientId);
        }

        private IInstanceRepository GetRepositoryByClientId(int clientId)
        {
            if (_repositories.TryGetValue(clientId, out var repository))
                return repository;

            var created = _produceRepository(clientId);
            _repositories.Add(clientId, created);
            return created;
        }
    }
}