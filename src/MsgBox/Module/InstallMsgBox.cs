namespace Files.Module
{
    using Bb.GenericIoc;
    using MsgBox;
    using System;

    public class InstallMsgBox : BaseIocRegister
    {

        public InstallMsgBox()
        {

        }

        public override void Register()
        {
            Register<IMessageBoxService, MessageBoxService>()
                .LifestyleTransient()
                ;
        }

    }

}