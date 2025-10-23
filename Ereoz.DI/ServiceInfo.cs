using Ereoz.Abstractions.DI;
using System;

namespace Ereoz.DI
{
    public class ServiceInfo : IServiceInfo
    {
        internal ServiceInfo(Type service, Func<object> factory)
        {
            ServiceType = service;
            Factory = factory;
        }

        public Type ServiceType { get; }
        public Func<object> Factory { get; }
        public bool IsSingletone { get; private set; }
        public object SingletoneInstance { get; set; }

        public void AsSingletone() =>
            IsSingletone = true;

        public void AsSingletone(object instance)
        {
            IsSingletone = true;
            SingletoneInstance = instance;
        }

        public void AsTransient() =>
            IsSingletone = false;
    }
}
