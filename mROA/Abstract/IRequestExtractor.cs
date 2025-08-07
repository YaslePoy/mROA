using System;
using System.Threading.Tasks;
using mROA.Implementation;

namespace mROA.Abstract
{
    public interface IRequestExtractor
    {
        Task StartExtraction();
        void PushMessage(object parsed, EMessageType originalType);
        Predicate<NetworkMessage> Rule { get; }
        Func<NetworkMessage, Type?>[] Converters { get; }
    }
}