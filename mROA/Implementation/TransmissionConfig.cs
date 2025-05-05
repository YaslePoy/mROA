using System;
using mROA.Abstract;

namespace mROA.Implementation
{
#pragma warning disable CS8618, CS9264
    public static class TransmissionConfig
    {
#if TRACE
        public static int TotalTransmittedBytes { get; set; }
#endif
        // private static IContextRepository? _realContextRepository;
        // private static IContextRepository? _remoteEndpointContextRepository;
        // private static IOwnershipRepository? _ownershipRepository;
        //
        // public static IContextRepository RealContextRepository
        // {
        //     get => _realContextRepository ?? throw new NullReferenceException("RealContextRepository is null");
        //     set => _realContextRepository = value;
        // }
        //
        // public static IContextRepository RemoteEndpointContextRepository
        // {
        //     get => _remoteEndpointContextRepository ??
        //            throw new NullReferenceException("RemoteEndpointContextRepository is null");
        //     set => _remoteEndpointContextRepository = value;
        // }
        //
        // public static IOwnershipRepository OwnershipRepository
        // {
        //     get => _ownershipRepository ?? throw new NullReferenceException("OwnershipRepository is null");
        //     set => _ownershipRepository = value;
        // }
    }
}