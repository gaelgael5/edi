namespace Edi.Themes
{

    using Bb.GenericIoc;
    using Edi.Themes.Interfaces;
    using System;

    public class InstallThemes : BaseIocRegister
    {

        public InstallThemes()
        {

        }

        public override void Register()
        {

            Register<IThemesManager, ThemesManager>()
                .LifestyleSingleton();

        }

    }

}