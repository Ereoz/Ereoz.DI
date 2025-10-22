using Ereoz.Abstractions.DI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ereoz.DI
{
    public class ServiceContainer : IServiceContainer
    {
        private Dictionary<Type, List<IServiceInfo>> _services;

        public ServiceContainer()
        {
            _services = new Dictionary<Type, List<IServiceInfo>>();
            Register<IServiceContainer, ServiceContainer>().AsSingletone();
        }

        public IServiceInfo Register<Service>() =>
            Register(typeof(Service));

        public IServiceInfo Register<Service>(Func<object> factory) =>
            Register(typeof(Service), factory);

        public IServiceInfo Register<Contract, Implementation>() =>
            Register(typeof(Contract), typeof(Implementation));

        public IServiceInfo Register<Contract, Implementation>(Func<object> factory) =>
            Register(typeof(Contract), typeof(Implementation), factory);

        public IServiceInfo Register(Type service) =>
            Register(service, factory: null);

        public IServiceInfo Register(Type contract, Type implementation) =>
            Register(contract, implementation, null);

        public IServiceInfo Register(Type service, Func<object> factory)
        {
            var serviceInfo = new ServiceInfo(service, factory);

            _services[service] = new List<IServiceInfo>() { serviceInfo };

            return serviceInfo;
        }

        public IServiceInfo Register(Type contract, Type implementation, Func<object> factory)
        {
            var serviceInfo = new ServiceInfo(implementation, factory);

            if (_services.TryGetValue(contract, out List<IServiceInfo> implementations))
                implementations.Add(serviceInfo);
            else
                _services.Add(contract, new List<IServiceInfo>() { serviceInfo });

            return serviceInfo;
        }

        public Service Resolve<Service>() =>
            (Service)Resolve(typeof(Service));

        public Service Resolve<Service>(string name) =>
            (Service)Resolve(typeof(Service), name);

        public object Resolve(Type service) =>
            Resolve(service, "");

        public object Resolve(Type service, string name)
        {
            if (service.IsGenericType && service.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return ResolveAllImplementations(service.GetGenericArguments()[0]);

            if (_services.TryGetValue(service, out List<IServiceInfo> services))
            {
                IServiceInfo serviceInfo = services.Where(it => it.ServiceType.Name.ToLower() == name.ToLower())
                                                   .FirstOrDefault() is ServiceInfo namedServiceInfo
                                                   ? namedServiceInfo
                                                   : services.Last();

                if (serviceInfo.IsSingletone)
                {
                    if (serviceInfo.SingletoneInstance == null)
                        serviceInfo.SingletoneInstance = serviceInfo.Factory != null ? serviceInfo.Factory() : ResolveInstance(serviceInfo.ServiceType);

                    return serviceInfo.SingletoneInstance;
                }
                else
                {
                    return serviceInfo.Factory != null ? serviceInfo.Factory() : ResolveInstance(serviceInfo.ServiceType);
                }
            }
            else
            {
                return ResolveInstance(service);
            }
        }

        public IEnumerable<Service> ResolveAllImplementations<Service>() =>
            ResolveAllImplementations(typeof(Service)).Cast<Service>();

        public IEnumerable<object> ResolveAllImplementations(Type contract)
        {
            IList<object> implementations = new List<object>();

            if (_services.TryGetValue(contract, out List<IServiceInfo> servicesInfo))
            {
                foreach (var serviceInfo in servicesInfo)
                    implementations.Add(Resolve(serviceInfo.ServiceType));
            }
            else
            {
                throw new ContractNotRegisteredException($"{contract} - is interface or abstract class and must be registered.");
            }

            MethodInfo castMethod = typeof(Enumerable)
                .GetMethod("Cast")
                .MakeGenericMethod(contract);

            return (IEnumerable<object>)castMethod.Invoke(null, new object[] { implementations });
        }

        private object ResolveInstance(Type service)
        {
            if (service.IsInterface)
                throw new ContractNotRegisteredException($"{service.FullName} - is interface and must be registered.");

            if (service.IsAbstract)
                throw new ContractNotRegisteredException($"{service.FullName} - is abstract class and must be registered.");

            object[] parameters = null;

            if (service.GetConstructors()
                       .Where(c => c.GetParameters().Length > 0)
                       .FirstOrDefault() is ConstructorInfo constructorWithParameters)
            {
                parameters = constructorWithParameters.GetParameters()
                                                      .Select(p => Resolve(p.ParameterType, p.Name))
                                                      .ToArray();

            }

            return Activator.CreateInstance(service, args: parameters);
        }
    }
}
