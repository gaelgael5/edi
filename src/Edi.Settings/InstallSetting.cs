namespace Edi.Settings
{
    using Bb.GenericIoc;
    using Edi.Settings.Interfaces;

    public class InstallSetting : BaseIocRegister
    {

        public InstallSetting()
        {

        }

        public override void Register()
        {

            Register<ISettingsManager, SettingsManagerImpl>()
                .LifestyleSingleton();

        }



       
    }


}