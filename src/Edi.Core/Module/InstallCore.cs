namespace Edi.Core.Module
{
    using Bb.GenericIoc;
    using Edi.Core.Interfaces;
    using Edi.Core.Interfaces.DocumentTypes;
    using Edi.Core.Models;
    using Edi.Core.Models.DocumentTypes;
    using Edi.Interfaces.App;
    using Edi.Interfaces.MessageManager;
    using System;

    public class InstallCore : BaseIocRegister
    {

        public InstallCore() 
            : base ("InstallIOutput")
        {

        }

        public override void Register()
        {

            Register<IAppCore, AppCore>()
                .LifestyleSingleton();
            
            Register<IToolWindowRegistry, ToolWindowRegistry>()
                .LifestyleSingleton();
            
            Register<IMessageManager, MessageManager>()
                .LifestyleSingleton();
            
            Register<IDocumentTypeManager, DocumentTypeManager>()
                .LifestyleSingleton()
            ;
        }

    }

}