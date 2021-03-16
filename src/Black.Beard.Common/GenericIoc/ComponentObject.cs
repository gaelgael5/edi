using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Bb.GenericIoc
{

    /// <summary>
    /// Store the ioc configuration before inject in the real ioc
    /// </summary>
    public class ComponentObject
    {

        public ComponentObject(ImplementationBase iocImplementation)
        {
            _implementation = iocImplementation;
            _configuration = new Dictionary<Type, IocRegisterConfiguration>();
            _iocRegisterIinstances = new List<BaseIocRegister>();

            AddInstaller(new GenericIocRegister<IServiceProvider>(_implementation));

        }


        public ComponentObject AddInstallers(IEnumerable<Type> installers)
        {

            foreach (Type installerType in installers)
            {
                BaseIocRegister instance = null;
                try
                {
                    instance = (BaseIocRegister)Activator.CreateInstance(installerType);
                }
                catch (Exception e)
                {
                    Trace.TraceError("Failed to create instance from {0}. {1}", installerType, e);
                }

                if (instance != null)
                    AddInstaller(instance);
            }

            return this;

        }

        public void AddInstaller(BaseIocRegister instance)
        {
            try
            {
                instance.Component = this;
                instance.ProviderService = _implementation;
                instance.Register();
                _iocRegisterIinstances.Add(instance);
            }
            catch (Exception e)
            {
                Trace.TraceError("Failed to execute register on instance from {0}. {1}", instance.GetType(), e);
            }
        }

        public ComponentObject ApplyBuild()
        {

            foreach (var item in this._configuration.Values)
                try
                {
                    _implementation.Process(item);
                    item.Builded = true;
                }
                catch (Exception e)
                {
                    Trace.TraceError("Failed to build configuration on instance from {0}. {1}", item.GetType(), e);
                }

            return this;

        }


        public ComponentObject Configure()
        {

            HashSet<string> _configured = new HashSet<string>();


            var items = _iocRegisterIinstances.ToList();
            List<BaseIocRegister> _toRemove = new List<BaseIocRegister>();
            List<BaseIocRegister> _toEnd = new List<BaseIocRegister>();

            while (items.Count > 0)
            {

                _toRemove.Clear();
                _toEnd.Clear();

                foreach (BaseIocRegister instance in items)
                    if (CanExecute(_configured, instance))
                    {
                        try
                        {
                            instance.Configure();
                            _toRemove.Add(instance);
                            _configured.Add(instance.Name);
                        }
                        catch (Exception e)
                        {
                            _toEnd.Add(instance);
                            Trace.TraceError("Failed to configure instance {0}. {1}", instance.GetType(), e);
                        }


                    }

                if (_toRemove.Count > 0)
                {
                    foreach (var item in _toRemove)
                        items.Remove(item);
                }
                else
                {
                    break;
                }

                if (_toEnd.Count > 0)
                    foreach (var item in _toEnd)
                    {
                        items.Remove(item);
                        items.Add(item);
                    }

            }

            return this;

        }



        private bool CanExecute(HashSet<string> configured, BaseIocRegister instance)
        {

            if (instance.Children.Any(c => !c.Builded))
                return false;

            if (instance.ConfigurationAfter.Length > 0)
                foreach (var item in instance.ConfigurationAfter)
                    if (!configured.Contains(item))
                        return false;

            return true;

        }

        public IocRegisterConfiguration Get<TInterface>(BaseIocRegister parent) where TInterface : class
        {

            if (!_configuration.TryGetValue(typeof(TInterface), out IocRegisterConfiguration config))
                _configuration
                    .Add(typeof(TInterface), config = new IocRegisterConfiguration(parent) { Name = typeof(TInterface).Name }
                                                            .For<TInterface>()
                );

            return config;

        }

        private readonly ImplementationBase _implementation;
        private readonly Dictionary<Type, IocRegisterConfiguration> _configuration;
        private readonly List<BaseIocRegister> _iocRegisterIinstances;
    }

}
