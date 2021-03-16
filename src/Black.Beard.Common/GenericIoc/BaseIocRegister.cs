using System;
using System.Collections.Generic;

namespace Bb.GenericIoc
{

    public abstract class BaseIocRegister
    {

        public BaseIocRegister(params string[] requireds)
        {
            this._children = new List<IocRegisterConfiguration>();
            this.Name = GetType().Name;
            ConfigurationAfter = requireds;
        }

        public string[] ConfigurationAfter { get; }
        
        public List<IocRegisterConfiguration> Children { get => this._children; }


        public string Name { get; protected set; }

        internal void AddConfiguration(IocRegisterConfiguration iocRegisterConfiguration)
        {
            this._children.Add(iocRegisterConfiguration);
        }

        public virtual void Configure()
        {

        }

        public abstract void Register();



        protected IocRegisterConfiguration Register<TImplmentation>()
           where TImplmentation : class
        {

            return Component.Get<TImplmentation>(this)
                .ImplementedBy<TImplmentation>()
            ;

        }

        protected IocRegisterConfiguration Register<TInterface, TImplmentation>()
            where TInterface : class
            where TImplmentation : class, TInterface
        {

            return Component.Get<TInterface>(this)
                .ImplementedBy<TImplmentation>()
            ;

        }

        protected IocRegisterConfiguration RegisterWithInstance<TInterface>(TInterface instance)
            where TInterface : class
        {
            return Component.Get<TInterface>(this)
                 .Instance(instance)
            ;
        }

        protected IocRegisterConfiguration RegisterWithFactory<TInterface>(Func<TInterface> instance)
            where TInterface : class
        {

            return Component.Get<TInterface>(this)
                .Factory(instance)
            ;
        }

        protected T GetService<T>()
        {
            return (T)ProviderService.GetService(typeof(T));
        }

        public ComponentObject Component { get; internal set; }

        public ImplementationBase ProviderService { get; internal set; }


        private readonly List<IocRegisterConfiguration> _children;

    }

}
