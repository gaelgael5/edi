namespace Output.Module
{
    using Edi.Core.Interfaces;
    using Edi.Core.Resources;
    using Edi.Core.View.Pane;
    using Edi.Core.ViewModels;
    using Edi.Interfaces.MessageManager;
    using Output.ViewModels;
    using System.Reflection;
    using System.Windows;
    using Bb.GenericIoc;
    using System;

    public class InstallIOutput : BaseIocRegister
    {

        public InstallIOutput()
        {

        }

        public override void Register()
        {
            Register<IOutput, OutputTWViewModel>()
                .LifestyleSingleton();
        }

        public override void Configure()
        {

            var avalonDockLayoutViewModel = GetService<IAvalonDockLayoutViewModel>();
            this.RegisterDataTemplates(avalonDockLayoutViewModel.ViewProperties.SelectPanesTemplate);

            var toolRegistry = GetService<IToolWindowRegistry>();
            var messageManager = GetService<IMessageManager>();

            if (toolRegistry != null && messageManager != null)
            {
                var toolVM = GetService<IOutput>();
                messageManager.RegisterOutputStream(toolVM);
                toolRegistry.RegisterTool(toolVM as ToolViewModel);
            }

        }


        #region registering methods
        /// <summary>
        /// Register viewmodel types with <seealso cref="DataTemplate"/> for a view
        /// and return all definitions with a <seealso cref="PanesTemplateSelector"/> instance.
        /// </summary>
        /// <param name="paneSel"></param>
        /// <returns></returns>
        private PanesTemplateSelector RegisterDataTemplates(PanesTemplateSelector paneSel)
        {
            // Register Log4Net DataTemplates
            var template = ResourceLocator.GetResource<DataTemplate>(
                                    Assembly.GetAssembly(typeof(OutputTWViewModel)).GetName().Name,
                                    "DataTemplates/OutputViewDataTemplate.xaml",
                                    "OutputViewDataTemplate") as DataTemplate;

            paneSel.RegisterDataTemplate(typeof(OutputTWViewModel), template);

            return paneSel;
        }
        #endregion registering methods
    }

}
