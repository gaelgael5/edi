namespace Edi
{
    using Bb.GenericIoc;
    using Edi.Apps.Interfaces;
    using Edi.Apps.ViewModels;
    using Edi.Apps.Views.Shell;
    using Edi.Core.Interfaces;
    using MRULib.MRU.Interfaces;
    using System;

    public class RootInstaller : BaseIocRegister
    {

        public RootInstaller()
        {

        }

        public override void Register()
        {

            // Register shell to have a MainWindow to start up with
            Register<Edi.Apps.IShell<MainWindow>, Edi.Apps.Shell>()
                .LifestyleTransient();

            // Register MainWindow to help castle satisfy Shell dependencies on it
            Register<MainWindow, MainWindow>()
                .LifestyleTransient();

            RegisterWithInstance<IMRUListViewModel>(MRULib.MRU_Service.Create_List())
                .LifestyleSingleton()
                ;

            Register<IAvalonDockLayoutViewModel, AvalonDockLayoutViewModel>()
                .LifestyleSingleton()
                ;
            
            Register<IApplicationViewModel, ApplicationViewModel>()
                .AndFor<IFileOpenService>()
                .LifestyleSingleton();

         
        }

    }


}