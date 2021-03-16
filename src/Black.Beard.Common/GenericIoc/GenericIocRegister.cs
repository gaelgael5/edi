using System;

namespace Bb.GenericIoc
{

    public class GenericIocRegister<TInterface> : BaseIocRegister
        where TInterface : class
    {

        public GenericIocRegister(TInterface implementation, Action configuration = null)
            : base()
        {
            this._implementation = implementation;
            this._configuration = configuration;
            this.Name = typeof(GenericIocRegister<TInterface>).GetGenericArguments()[0].Name;
        }
        

        public override void Configure()
        {
            if (this._configuration != null)
                this._configuration();
        }

        public override void Register()
        {
            RegisterWithInstance(_implementation);
        }


        private TInterface _implementation;
        private Action _configuration;


    }

}
