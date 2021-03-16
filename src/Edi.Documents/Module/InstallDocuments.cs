using Bb.GenericIoc;
using System;

namespace Edi.Documents.Module
{

    public class InstallDocuments : BaseIocRegister
    {

        public InstallDocuments()
        {

        }


        public override void Register()
        {
            Register<InstallModule>()
                .LifestyleTransient();
        }


        public override void Configure()
        {

            InstallModule module = GetService<InstallModule>();
            module.Initialize();
        }


    }

}
