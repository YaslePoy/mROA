using System;

namespace mROA.Implementation.Frontend
{
    public class RemoteException : Exception
    {
        public Guid CallRequestId;
        private readonly string _error;

        public RemoteException(string error)
        {
            _error = error;
        }

        public override string Message => $"Error in request {CallRequestId} : {_error}";
    }
}