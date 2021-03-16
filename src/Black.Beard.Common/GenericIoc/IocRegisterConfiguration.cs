using System;
using System.Collections.Generic;

namespace Bb.GenericIoc
{

    public class IocRegisterConfiguration
    {

        public IocRegisterConfiguration(BaseIocRegister parent)
        {
            this.TypeKeys = new List<Type>();
            parent.AddConfiguration(this);
        }

        public bool Builded { get; set; }

        public string Name { get; internal set; }

        public Type TypeKey { get; private set; }

        public List<Type> TypeKeys { get; }

        public Type TypeImplemented { get; private set; }

        public LifeCycleEnum LifeCycle { get; private set; }

        public object InstanceService { get; private set; }

        public object InstanceFactory { get; private set; }

        public IocRegisterConfiguration LifestyleSingleton()
        {
            this.LifeCycle = LifeCycleEnum.Singleton;
            return this;
        }

        public IocRegisterConfiguration LifestylePooled()
        {
            this.LifeCycle = LifeCycleEnum.Pooled;
            return this;
        }

        public IocRegisterConfiguration LifestyleScoped()
        {
            this.LifeCycle = LifeCycleEnum.Scoped;
            return this;
        }

        public IocRegisterConfiguration LifestyleTransient()
        {
            this.LifeCycle = LifeCycleEnum.Transient;
            return this;
        }



        public IocRegisterConfiguration For<TInterface>()
            where TInterface : class
        {
            this.TypeKeys.Add(typeof(TInterface));
            this.TypeKey = typeof(TInterface);
            return this;
        }

        public IocRegisterConfiguration AndFor<TInterface>()
          where TInterface : class
        {
            this.TypeKeys.Add(typeof(TInterface));
            return this;
        }

        internal IocRegisterConfiguration ImplementedBy<TImplmentation>()
            where TImplmentation : class
        {
            this.TypeImplemented = typeof(TImplmentation);
            return this;
        }

        internal IocRegisterConfiguration Instance<TImplmentation>(TImplmentation instance)
            where TImplmentation : class
        {
            this.InstanceService = instance;
            return this;
        }

        internal IocRegisterConfiguration Factory<TInterface>(Func<TInterface> factory)
            where TInterface : class
        {
            this.InstanceFactory = factory;
            return this;
        }

    }

    public enum LifeCycleEnum
    {
        Undefined,
        Transient,
        Singleton,
        Scoped,
        Pooled,
    }

}
