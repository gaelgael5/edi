namespace Files.Module
{
    using Bb.GenericIoc;
    using System;

    public class InstallFile : BaseIocRegister
    {

        public InstallFile()
        {

        }

        public override void Register()
        {
            Register<InstallModule, InstallModule>()
                .LifestyleSingleton()
                ;
        }

        public override void Configure()
        {
            var module = GetService<InstallModule>();
            module.Initialize();
        }


    }

}