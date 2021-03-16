namespace Edi.Settings
{
    using Bb.GenericIoc;
    using MLib.Interfaces;
    using MLib.Internal;

    public class InstallMlib : BaseIocRegister
    {

        public InstallMlib()
        {

        }

        public override void Register()
        {

            Register<IAppearanceManager, AppearanceManagerImpl>()
                .LifestyleSingleton();

        }

       
    }


}