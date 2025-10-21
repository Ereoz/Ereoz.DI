using System;

namespace Ereoz.DI
{
    public sealed class ContractNotRegisteredException : Exception
    {
        public ContractNotRegisteredException(string message)
            : base(message) { }
    }
}
