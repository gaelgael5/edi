﻿namespace Edi.Apps.Views
{
    using System;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Threading;
    using Edi.Interfaces.Events;
    using AvalonDock;
    using AvalonDock.Layout;
    using AvalonDock.Layout.Serialization;

    /// <summary>
    /// Interaction logic for AvalonDockView.xaml
    /// </summary>
    [TemplatePartAttribute(Name = PART_DockView, Type = typeof(DockingManager))]
	public partial class AvalonDockView : UserControl
	{
        #region fields
        /// <summary>
        /// defines the nme of the required <see cref="DockingManager"/> control in the attached control XAML
        /// </summary>
        const string PART_DockView = "PART_DockView";

        protected static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private DockingManager _PART_DockView;

		private DataTemplateSelector _LayoutItemTemplateSelector;
		private DataTemplate _DocumentHeaderTemplate;
		private StyleSelector _LayoutItemContainerStyleSelector;
		private ILayoutUpdateStrategy _LayoutUpdateStrategy;

		private string _mOnLoadXmlLayout;
		#endregion fields

		#region constructor
		/// <summary>
		/// Static constructor
		/// </summary>
		static AvalonDockView()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(AvalonDockView),
								new FrameworkPropertyMetadata(typeof(AvalonDockView)));
		}

		/// <summary>
		/// Class Constructor
		/// </summary>
		public AvalonDockView()
		{
			LayoutId = Guid.NewGuid();
		}
		#endregion constructor

		#region properties
		/// <summary>
		/// Gets/Sets the LayoutId of the AvalonDocking Manager layout used to manage
		/// the positions and layout of documents and tool windows within the AvalonDock
		/// view.
		/// </summary>
		public Guid LayoutId
		{
			get;
			private set;
		}

		/// <summary>
		/// Gets the current AvalonDockManager Xml layout and returns it as a string.
		/// </summary>
		public string CurrentAdLayout
		{
			get
			{
				if (_PART_DockView == null)
					return String.Empty;

				string xmlLayoutString = string.Empty;
				try
				{
					using (StringWriter fs = new StringWriter())
					{
						XmlLayoutSerializer xmlLayout = new XmlLayoutSerializer(_PART_DockView);

						xmlLayout.Serialize(fs);

						xmlLayoutString = fs.ToString();
					}
				}
				catch
				{
				}

				return xmlLayoutString;
			}
		}
		#endregion properties

		#region methods
		/// <summary>
		/// Standard method is executed when control template is applied to lookless control.
		/// </summary>
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

            Loaded += AvalonDockView_Loaded;
		}

        /// <summary>
        /// Class Constructor
        /// </summary>
        /// <param name="paneSel"></param>
        /// <param name="documentHeaderTemplate"></param>
        /// <param name="panesStyleSelector"></param>
        /// <param name="layoutInitializer"></param>
        /// <param name="layoutId"></param>
        public void InitTemplates(DataTemplateSelector paneSel,
								  DataTemplate documentHeaderTemplate,
								  StyleSelector panesStyleSelector,
								  ILayoutUpdateStrategy layoutInitializer,
								  Guid layoutId
								)
		{
			_LayoutItemTemplateSelector = paneSel;
			_DocumentHeaderTemplate = documentHeaderTemplate;
			_LayoutItemContainerStyleSelector = panesStyleSelector;
			_LayoutUpdateStrategy = layoutInitializer;
			LayoutId = layoutId;

			if (_PART_DockView == null)
				return;

			SetCustomLayoutItems();
		}

		#region Workspace Layout Management
		/// <summary>
		/// Is executed when subscriber (viewmodel) sends an Xml layout string
        /// notification that wants to receive the load <seealso cref="LoadLayoutEvent"/>.
		/// 
		/// Save layout is triggered by the containing window onClosed event.
		/// </summary>
		/// <param name="args"></param>
		public void OnLoadLayout(object sender, LoadLayoutEventArgs args)
		{
			if (args == null)
				return;

			if (string.IsNullOrEmpty(args.XmlLayout))
				return;

			_mOnLoadXmlLayout = args.XmlLayout;

			if (_PART_DockView == null)
				return;

			LoadXmlLayout(_mOnLoadXmlLayout);
		}

		private void LoadXmlLayout(string xmlLayout)
		{
			try
			{
				StringReader sr = new StringReader(xmlLayout);

				XmlLayoutSerializer layoutSerializer = null;

				Application.Current.Dispatcher.Invoke(new Action(() =>
				{
					try
					{
						layoutSerializer = new XmlLayoutSerializer(_PART_DockView);
						layoutSerializer.LayoutSerializationCallback += UpdateLayout;
						layoutSerializer.Deserialize(sr);
					}
					catch (Exception exp)
					{
						Logger.ErrorFormat("Error Loading Layout: {0}\n\n{1}", exp.Message, xmlLayout);
					}

				}), DispatcherPriority.Background);
			}
			catch (Exception exp)
			{
				Logger.ErrorFormat("Error Loading Layout: {0}\n\n{1}", exp.Message, xmlLayout);
			}
		}

		/// <summary>
		/// Convert a Avalondock ContentId into a viewmodel instance
		/// that represents a document or tool window. The re-load of
		/// this component is cancelled if the Id cannot be resolved.
		/// 
		/// The result is (viewmodel Id or Cancel) is returned in <paramref name="args"/>.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private void UpdateLayout(object sender, LayoutSerializationCallbackEventArgs args)
		{
			try
			{
				Core.Interfaces.IViewModelResolver resolver = null;

				resolver = DataContext as Core.Interfaces.IViewModelResolver;

				if (resolver == null)
					return;

				// Get a matching viewmodel for a view through DataContext of this view
				var contentViewModel = resolver.ContentViewModelFromId(args.Model.ContentId);

				if (contentViewModel == null)
					args.Cancel = true;

				// found a match - return it
				args.Content = contentViewModel;
			}
			catch (Exception exp)
			{
				Console.WriteLine(exp.Message);
			}
		}
        #endregion Workspace Layout Management

        /// <summary>
        /// Method invokes when control is loaded (is visible in UI)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AvalonDockView_Loaded(object sender, RoutedEventArgs e)
        {
            _PART_DockView = Template.FindName("PART_DockView", this) as DockingManager;

            SetCustomLayoutItems();
            ////this.LoadXmlLayout(this.mOnLoadXmlLayout);
        }

        /// <summary>
        /// Assigns the currently assigned custom layout controls to the AvalonDock DockingManager.
        /// </summary>
        private void SetCustomLayoutItems()
		{
			if (_PART_DockView == null)
				return;

			if (_LayoutItemTemplateSelector != null)
				_PART_DockView.LayoutItemTemplateSelector = _LayoutItemTemplateSelector;

			if (_DocumentHeaderTemplate != null)
				_PART_DockView.DocumentHeaderTemplate = _DocumentHeaderTemplate;

			if (_LayoutItemContainerStyleSelector != null)
				_PART_DockView.LayoutItemContainerStyleSelector = _LayoutItemContainerStyleSelector;

			if (_LayoutUpdateStrategy != null)
				_PART_DockView.LayoutUpdateStrategy = _LayoutUpdateStrategy;
		}
		#endregion methods
	}
}
