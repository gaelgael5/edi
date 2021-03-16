using System;

namespace Bb.GenericIoc
{
    public abstract class ImplementationBase : IServiceProvider
    {


        public abstract void Process(IocRegisterConfiguration item);

        public abstract object GetService(Type serviceType);

    }

}
