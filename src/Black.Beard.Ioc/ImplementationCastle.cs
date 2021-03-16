using System;
using System.ComponentModel;
using Castle.Windsor;
using Castle.Windsor.Installer;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using reg = Castle.MicroKernel.Registration;
using System.Reflection;

namespace Bb.GenericIoc
{
    public class ImplementationCastle : ImplementationBase
    {

        public ImplementationCastle()
        {
            _container = new WindsorContainer();
        }

        public override void Process(IocRegisterConfiguration item)
        {

            ComponentRegistration c = reg.Component.For(item.TypeKeys.ToArray());

            if (item.TypeImplemented != null)
                c.ImplementedBy(item.TypeImplemented);

            else if (item.InstanceService != null)
                c.Instance(item.InstanceService);

            else if (item.InstanceFactory != null)
            {

                var argTypes = item.InstanceFactory.GetType().GetGenericArguments();
                Type tFactory = argTypes[0];

                var m = typeof(ComponentRegistration)
                    .GetMethod("UsingFactory", BindingFlags.Public | BindingFlags.Instance)
                    .MakeGenericMethod(tFactory, item.TypeKey);

                m.Invoke(c, new object[] { item.InstanceFactory });

            }
            else
            {

            }

            switch (item.LifeCycle)
            {
                case LifeCycleEnum.Transient:
                    c.LifestyleTransient();
                    break;
                case LifeCycleEnum.Singleton:
                    c.LifestyleSingleton();
                    break;
                case LifeCycleEnum.Scoped:
                    c.LifestyleScoped();

                    break;
                case LifeCycleEnum.Pooled:
                    c.LifestylePooled();
                    break;

                case LifeCycleEnum.Undefined:
                default:
                    break;
            }

            _container.Register(c);

        }

        public override object GetService(Type serviceType)
        {
            return _container.Resolve(serviceType);
        }


        private readonly IWindsorContainer _container;

        private class serviceProvider : IServiceProvider
        {
            public object GetService(Type serviceType)
            {
                throw new NotImplementedException();
            }

        }

    }

}
