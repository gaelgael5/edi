namespace Edi.Apps.ViewModels
{
    using Enums;
    using Core.Interfaces;
    using Core.Interfaces.Documents;
    using Core.Interfaces.DocumentTypes;
    using Core.Models.Documents;
    using Core.ViewModels;
    using Core.ViewModels.Base;
    using Core.ViewModels.Command;
    using Core.ViewModels.Events;
    using Dialogs.About;
    using Documents.ViewModels.EdiDoc;
    using Documents.ViewModels.MiniUml;
    using Documents.ViewModels.StartPage;
    using Settings.Interfaces;
    using SettingsView.Config.ViewModels;
    using Files.ViewModels.RecentFiles;
    using Microsoft.Win32;
    using MRULib.MRU.Interfaces;
    using MiniUML.Model.ViewModels.Document;
    using MsgBox;
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Threading;
    using Edi.Apps.Interfaces;
    using Edi.Core.Models;
    using Edi.Interfaces.MessageManager;
    using Edi.Interfaces.App;
    using Edi.Themes.Interfaces;

    /// <summary>
    /// This class manages the complete application life cyle from start to end.
    /// It publishes the methodes, properties, and events necessary to integrate
    /// the application into a given shell (BootStrapper, App.xaml.cs etc).
    /// 
    /// Installs in Edi's main project installer
    /// </summary>
    public partial class ApplicationViewModel : ViewModelBase,
                                                IViewModelResolver,
                                                IApplicationViewModel,
                                                IDocumentParent,
                                                IFileOpenService
    {
        #region fields
        public const string Log4NetFileExtension = "log4j";
        public static readonly string Log4NetFileFilter = Util.Local.Strings.STR_FileType_FileFilter_Log4j;

        public const string MiniUmlFileExtension = "uml";
        public static readonly string UmlFileFilter = Util.Local.Strings.STR_FileType_FileFilter_UML;

/*
        private static string _ediTextEditorFileFilter =
                                    Util.Local.Strings.STR_FileType_FileFilter_AllFiles +
                                    "|" + Util.Local.Strings.STR_FileType_FileFilter_TextFiles +
                                    "|" + Util.Local.Strings.STR_FileType_FileFilter_CSharp +
                                    "|" + Util.Local.Strings.STR_FileType_FileFilter_HTML +
                                    "|" + Util.Local.Strings.STR_FileType_FileFilter_SQL;
*/

        protected static readonly log4net.ILog Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private bool? _DialogCloseResult;
        private bool? _IsNotMaximized;
        private bool _IsWorkspaceAreaOptimized;

        private bool _ShutDownInProgress;
        private bool _ShutDownInProgressCancel;

        private readonly ObservableCollection<IFileBaseViewModel> _Files;
        private ReadOnlyObservableCollection<IFileBaseViewModel> _ReadonyFiles;

        private IFileBaseViewModel _ActiveDocument;
        private ICommand _MainWindowActivated;

	    private readonly IAppCore _AppCore;
	    private readonly IToolWindowRegistry _ToolRegistry;
        private readonly ISettingsManager _SettingsManager;
	    private readonly IMessageManager _MessageManager;

        private readonly IDocumentTypeManager _DocumentTypeManager;
        private IDocumentType _SelectedOpenDocumentType;

        private readonly IMessageBoxService _MsgBox;
        private readonly IMRUListViewModel _MruVM;

        private readonly object _mLock = new object();
        private bool _IsMainWindowActivationProcessed;
        private bool _IsMainWindowActivationProcessingEnabled;
        #endregion fields

        #region constructor
        /// <summary>
        /// Constructor
        /// </summary>
        public ApplicationViewModel(IAppCore appCore,
                                    IAvalonDockLayoutViewModel avLayout,
                                    IToolWindowRegistry toolRegistry,
                                    IMessageManager messageManager,
                                    ISettingsManager programSettings,
                                    IThemesManager themesManager,
                                    IDocumentTypeManager documentTypeManager,
                                    IMRUListViewModel mru,
                                    IMessageBoxService msgBox)
            : this()
        {
	        _AppCore = appCore;
            AdLayout = avLayout;

	        _MessageManager = messageManager;
            _MsgBox = messageManager._MsgBox ?? msgBox;
            _MruVM = mru;

            _ToolRegistry = toolRegistry;

            // Subscribe to tool window manager who will relay the fact
            // that a tool windows have been registered and can now be published
            _ToolRegistry.PublishToolWindows += OnPublisToolWindows;

            _SettingsManager = programSettings;
            ApplicationThemes = themesManager;
            _DocumentTypeManager = documentTypeManager;
        }

        protected ApplicationViewModel()
        {
            _MruVM = null;
            AdLayout = null;
            _Files = new ObservableCollection<IFileBaseViewModel>();

            // Subscribe to publsihers who relay the fact that a new tool window has been registered
            // Register this methods to receive PRISM event notifications
/////            RegisterToolWindowEvent.Instance.Subscribe(OnRegisterToolWindow, ThreadOption.BackgroundThread);
        }
        #endregion constructor

        #region events
        /// <summary>
        /// Raised when this workspace should be removed from the UI.
        /// </summary>
        public event EventHandler RequestClose;

        /// <summary>
        /// The document with the current input focus has changed when this event fires.
        /// </summary>
        public event DocumentChangedEventHandler ActiveDocumentChanged;
        #endregion events

        #region Properties
        /// <summary>
        /// Gets an instance of the current application theme manager.
        /// </summary>
        public IThemesManager ApplicationThemes { get; }

        /// <summary>
        /// Gets a property to a <seealso cref="ICommand"/> that executes
        /// when the user activates the mainwindow (eg: does ALT+TAB between applications).
        /// This event is used to check whether a file has changed in the meantime or not.
        /// </summary>
        public ICommand MainWindowActivated
        {
            get
            {
	            return _MainWindowActivated ?? (_MainWindowActivated = new RelayCommand<object>((p) =>
	            {
		            // Is processing of this event currently enabled?
		            if (_IsMainWindowActivationProcessingEnabled == false)
			            return;

		            // Is this event already currently being processed?
		            if (_IsMainWindowActivationProcessed)
			            return;

		            lock (_mLock)
		            {
			            try
			            {
				            if (_IsMainWindowActivationProcessed)
					            return;

				            _IsMainWindowActivationProcessed = true;

				            foreach (var item in Files)
				            {
					            if (item.WasChangedExternally)
					            {
						            var result = _MsgBox.Show(
							            $"File '{item.FileName}' was changed externally. Click OK to reload or Cancel to keep current content.",
							            "File changed externally", MsgBoxButtons.OKCancel);

						            if (result == MsgBoxResult.OK)
						            {
							            item.ReOpen();
						            }
					            }
				            }
			            }
			            catch (Exception exp)
			            {
				            Logger.Error(exp.Message, exp);
				            _MsgBox.Show(exp, Util.Local.Strings.STR_MSG_IssueTrackerTitle, MsgBoxButtons.OK, MsgBoxImage.Error,
					            MsgBoxResult.NoDefaultButton,
					            _AppCore.IssueTrackerLink, _AppCore.IssueTrackerLink,
					            Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
			            }
			            finally
			            {
				            _IsMainWindowActivationProcessed = false;
			            }
		            }
	            }));
            }
        }

        #region ActiveDocument
        /// <summary>
        /// Gets/sets the dcoument that is currently active (has input focus) - if any.
        /// </summary>
        public IFileBaseViewModel ActiveDocument
        {
            get { return _ActiveDocument; }

	        set
            {
                if (_ActiveDocument != value)
                {
                    _ActiveDocument = value;

                    RaisePropertyChanged(() => ActiveDocument);
                    RaisePropertyChanged(() => ActiveEdiDocument);
                    RaisePropertyChanged(() => vm_DocumentViewModel);

                    // Ensure that no pending calls are in the dispatcher queue
                    // This makes sure that we are blocked until bindings are re-established
                    // (Bindings are, for example, required to scroll a selection into view for search/replace)
                    Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.SystemIdle, (Action)delegate
                    {
                        if (ActiveDocumentChanged != null)
                        {
                            ActiveDocumentChanged(this, new DocumentChangedEventArgs(_ActiveDocument)); //this.ActiveDocument

                            if (value != null && _ShutDownInProgress == false)
                            {
                                if (value.IsFilePathReal)
                                    _SettingsManager.SessionData.LastActiveFile = value.FilePath;
                            }
                        }
                    });
                }
            }
        }

        /// <summary>
        /// This is a type safe ActiveDocument property that is used to bind
        /// to an ActiveDocument of type <seealso cref="EdiViewModel"/>.
        /// This property returns null (thus avoiding binding errors) if the
        /// ActiveDocument is not of <seealso cref="EdiViewModel"/> type.
        /// </summary>
        public EdiViewModel ActiveEdiDocument => _ActiveDocument as EdiViewModel;

	    /// <summary>
        /// This is a type safe ActiveDocument property that is used to bind
        /// to an ActiveDocument of type <seealso cref="DocumentViewModel"/>.
        /// This property returns null (thus avoiding binding errors) if the
        /// ActiveDocument is not of <seealso cref="DocumentViewModel"/> type.
        /// 
        /// This particular property is also required to load MiniUML Plugins.
        /// </summary>
        public AbstractDocumentViewModel vm_DocumentViewModel
        {
            get
            {
	            MiniUmlViewModel vm = _ActiveDocument as MiniUmlViewModel;
	            return vm?.DocumentMiniUml;
            }
        }
        #endregion

        public IDocumentType SelectedOpenDocumentType
        {
            get { return _SelectedOpenDocumentType; }

	        private set
            {
	            if (_SelectedOpenDocumentType == value) return;
	            _SelectedOpenDocumentType = value;
	            RaisePropertyChanged(() => SelectedOpenDocumentType);
            }
        }

        public ObservableCollection<IDocumentType> DocumentTypes
        {
            get { return _DocumentTypeManager.DocumentTypes; }
        }

        /// <summary>
        /// Principable data source for collection of documents managed in the the document manager (of AvalonDock).
        /// </summary>
        public ReadOnlyObservableCollection<IFileBaseViewModel> Files
        {
            get { return _ReadonyFiles ?? (_ReadonyFiles = new ReadOnlyObservableCollection<IFileBaseViewModel>(_Files)); }
        }

        /// <summary>
        /// Principable data source for collection of tool window viewmodels
        /// whos view templating is managed in the the document manager of AvalonDock.
        /// </summary>
        public ObservableCollection<ToolViewModel> Tools
        {
            get { return _ToolRegistry.Tools; }
        }

	    public RecentFilesTWViewModel RecentFiles
        {
            get
            {
                var ret = GetToolWindowVm<RecentFilesTWViewModel>();
                return ret;
            }
        }

        /// <summary>
        /// Expose command to load/save AvalonDock layout on application startup and shut-down.
        /// </summary>
        public IAvalonDockLayoutViewModel AdLayout { get; }

	    public bool ShutDownInProgressCancel
		{
			get { return _ShutDownInProgressCancel; }

			set { _ShutDownInProgressCancel = value; }
		}

		/// <summary>
		/// Get the name of this application in a human read-able fashion
		/// </summary>
		public string ApplicationTitle { get { return _AppCore.AssemblyTitle; } }

        /// <summary>
        /// Convienance property to filter (cast) documents that represent
        /// actual text documents out of the general documents collection.
        /// 
        /// Items such as start page or program settings are not considered
        /// documents in this collection.
        /// </summary>
        private List<EdiViewModel> Documents => _Files.OfType<EdiViewModel>().ToList();
        #endregion Properties

        #region methods
        /// <summary>
        /// Activates/deactivates processing of the mainwindow activated event.
        /// </summary>
        /// <param name="bActivate"></param>
        public void EnableMainWindowActivated(bool bActivate)
        {
            _IsMainWindowActivationProcessingEnabled = bActivate;
        }

        #region OpenCommand
        /// <summary>
        /// Open a type of document from file persistence with dialog and user interaction.
        /// 
        /// So user select file(s) in a standard open file dialog and
        /// Edi opens all the files in one go.
        /// </summary>
        /// <param name="typeOfDocument"></param>
        private void OnOpen(string typeOfDocument = "")
        {
            try
            {
                var dlg = new OpenFileDialog();

                // Get filter strings for document specific filters or all filters
                // depending on whether type of document is set to a key or not.
                var fileEntries = _DocumentTypeManager.GetFileFilterEntries(typeOfDocument);
                dlg.Filter = fileEntries.GetFilterString();

                dlg.Multiselect = true;
                dlg.InitialDirectory = GetDefaultPath();

                if (dlg.ShowDialog().GetValueOrDefault())
                {
                    // Smallest value in filterindex is 1
                    var fo = fileEntries.GetFileOpenMethod(dlg.FilterIndex - 1);

                    foreach (string fileName in dlg.FileNames)
                    {
                        // Verify whether file is already open in editor, and if so, show it
                        IFileBaseViewModel fileViewModel = Documents.FirstOrDefault(fm => fm.FilePath == fileName);

                        if (fileViewModel != null) // File is already open so show it to the user
                        {
                            ActiveDocument = fileViewModel;
                            continue;
                        }

                        var dm = new DocumentModel();
                        dm.SetFileNamePath(fileName, true);

                        // Execute file open method from delegate and integrate new viewmodel instance
                        var vm = fo(dm, _SettingsManager, _MsgBox);

                        IntegrateDocumentVm(vm, fileName, true);
                    }

                    // Pre-select this document type in collection of document types that can be opened and viewed
                    var typeOfDocKey = _DocumentTypeManager.FindDocumentTypeByKey(typeOfDocument);
                    if (typeOfDocKey != null)
                        SelectedOpenDocumentType = typeOfDocKey;
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp.Message, exp);
                _MsgBox.Show(exp, Util.Local.Strings.STR_MSG_IssueTrackerTitle, MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
                             _AppCore.IssueTrackerLink,
                             _AppCore.IssueTrackerLink,
                             Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
            }
        }

        /// <summary>
        /// Wrapper method for file open
        /// - is executed when a file open is requested from external party such as tool window.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public bool FileOpen(string file)
        {
            return (Open(file) != null);
        }

	    /// <summary>
	    /// Open a file supplied in <paramref name="filePath"/> (without displaying a file open dialog).
	    /// </summary>
	    /// <param name="filePath">file to open</param>
	    /// <param name="closeDocumentWithoutMessageOnError"></param>
	    /// <param name="addIntoMru">indicate whether file is to be added into MRU or not</param>
	    /// <param name="typeOfDoc"></param>
	    /// <returns></returns>
	    public IFileBaseViewModel Open(string filePath,
                                       CloseDocOnError closeDocumentWithoutMessageOnError = CloseDocOnError.WithUserNotification,
                                       bool addIntoMru = true,
                                       string typeOfDoc = "EdiTextEditor")
        {
            Logger.InfoFormat("TRACE EdiViewModel.Open param: '{0}', AddIntoMRU {1}", filePath, addIntoMru);

            SelectedOpenDocumentType = DocumentTypes[0];

            // Verify whether file is already open in editor, and if so, show it
            IFileBaseViewModel fileViewModel = Documents.FirstOrDefault(fm => fm.FilePath == filePath);

            if (fileViewModel != null) // File is already open so show it to the user
            {
                ActiveDocument = fileViewModel;
                return fileViewModel;
            }

            IDocumentModel dm = new DocumentModel();
            dm.SetFileNamePath(filePath, true);

            // 1st try to find a document type handler based on the supplied extension
            var docType = _DocumentTypeManager.FindDocumentTypeByExtension(dm.FileExtension, true) ??
                          _DocumentTypeManager.FindDocumentTypeByKey(typeOfDoc);

            // 2nd try to find a document type handler based on the name of the prefered viewer
            // (Defaults to EdiTextEditor if no name is given)

	        if (docType != null)
            {
                fileViewModel = docType.FileOpenMethod(dm, _SettingsManager, _MsgBox);
            }
            else
            {
                ////if ((dm.FileExtension == string.Format(".{0}", ApplicationViewModel.MiniUMLFileExtension) && typeOfDoc == "EdiTextEditor") || typeOfDoc == "UMLEditor")
                ////{
                ////	fileViewModel = MiniUmlViewModel.LoadFile(filePath);
                ////}
                ////else
                ////{
                bool closeOnErrorWithoutMessage = closeDocumentWithoutMessageOnError == CloseDocOnError.WithoutUserNotification;

                // try to load a standard text file from the file system as a fallback method
                fileViewModel = EdiViewModel.LoadFile(dm, _SettingsManager, _MsgBox, closeOnErrorWithoutMessage);
                ////}
            }

            return IntegrateDocumentVm(fileViewModel, filePath, addIntoMru);
        }

        private IFileBaseViewModel IntegrateDocumentVm(IFileBaseViewModel fileViewModel,
                                                        string filePath,
                                                        bool addIntoMru)
        {
            if (fileViewModel == null)
            {
                if (_MruVM.FindEntry(filePath) != null)
                {
                    if (_MsgBox.Show(string.Format(Edi.Util.Local.Strings.STR_ERROR_LOADING_FILE_MSG, filePath),
                                                   Edi.Util.Local.Strings.STR_ERROR_LOADING_FILE_CAPTION, MsgBoxButtons.YesNo) == MsgBoxResult.Yes)
                    {
                        _MruVM.RemoveEntry(filePath);
                    }
                }

                return null;
            }

            fileViewModel.DocumentEvent += this.ProcessDocumentEvent;
            fileViewModel.ProcessingResultEvent += this.Vm_ProcessingResultEvent;
            _Files.Add(fileViewModel);

            // reset viewmodel options in accordance to current program settings

            if (fileViewModel is IDocumentEdi)
            {
                IDocumentEdi ediVM = fileViewModel as IDocumentEdi;
                this.SetActiveDocumentOnNewFileOrOpenFile(ediVM);
            }
            else
            {
                this.SetActiveFileBaseDocument(fileViewModel);
            }

            if (addIntoMru == true)
                this.GetToolWindowVm<RecentFilesTWViewModel>().AddNewEntryIntoMRU(filePath);

            return fileViewModel;
        }

        /// <summary>
        /// <seealso cref="IViewModelResolver"/> method for resolving
        /// AvalonDock contentid's against a specific viewmodel.
        /// </summary>
        /// <param name="contentId"></param>
        /// <returns></returns>
        public object ContentViewModelFromId(string contentId)
        {
            // Query for a tool window and return it
            var anchorableVm = Tools.FirstOrDefault(d => d.ContentId == contentId);


            if (anchorableVm is IRegisterableToolWindow)
            {
                IRegisterableToolWindow registerTw = anchorableVm as IRegisterableToolWindow;

                registerTw.SetDocumentParent(this);
            }

            if (anchorableVm != null)
                return anchorableVm;

            // Query for a matching document and return it
            if (_SettingsManager.SettingData.ReloadOpenFilesOnAppStart)
                return ReloadDocument(contentId);

            return null;
        }
        #endregion OnOpen

        #region NewCommand
        private void OnNew(TypeOfDocument t = TypeOfDocument.EdiTextEditor)
        {
            try
            {
                var typeOfDocKey = _DocumentTypeManager.FindDocumentTypeByKey(t.ToString());
                if (typeOfDocKey != null)
                {
                    var dm = new DocumentModel();

                    // Does this document type support creation of new documents?
                    if (typeOfDocKey.CreateDocumentMethod != null)
                    {
                        IFileBaseViewModel vm = typeOfDocKey.CreateDocumentMethod(dm, _MsgBox);

                        if (vm is IDocumentEdi)              // Process Edi ViewModel specific items
                        {
                            var ediVm = vm as IDocumentEdi;

                            ediVm.InitInstance(_SettingsManager.SettingData);

                            ediVm.IncreaseNewCounter();
                            ediVm.DocumentEvent += ProcessDocumentEvent;

                            ediVm.ProcessingResultEvent += Vm_ProcessingResultEvent;
                            ediVm.CreateNewDocument();

                            _Files.Add(ediVm);
                            SetActiveDocumentOnNewFileOrOpenFile(ediVm);
                        }
                        else
                            throw new NotSupportedException($"Creating Documents of type: '{t.ToString()}'");
                    }
                    else
                    {
                        // Modul registration with PRISM is missing here
                        if (t == TypeOfDocument.UmlEditor)
                        {
                            var umlVm = new MiniUmlViewModel(dm, _MsgBox);

                            umlVm.DocumentEvent += ProcessDocumentEvent;
                            _Files.Add(umlVm);
                            SetActiveFileBaseDocument(umlVm);
                        }
                        else
                            throw new NotSupportedException($"Creating Documents of type: '{t.ToString()}'");
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp.Message, exp);
                _MsgBox.Show(exp, Util.Local.Strings.STR_MSG_UnknownError_Caption,
                             MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
                             _AppCore.IssueTrackerLink,
                             _AppCore.IssueTrackerLink,
                             Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
            }
        }
        #endregion NewCommand

        #region Application_Exit_Command
        private void AppExit_CommandExecuted()
        {
            try
            {
                if (Closing_CanExecute() == false)
                    return;

	            _ShutDownInProgressCancel = false;
	            OnRequestClose();
            }
            catch (Exception exp)
            {
                Logger.Error(exp.Message, exp);
                _MsgBox.Show(exp, Util.Local.Strings.STR_MSG_UnknownError_Caption,
                             MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
                             _AppCore.IssueTrackerLink,
                             _AppCore.IssueTrackerLink,
                             Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
            }
        }
        #endregion Application_Exit_Command

        private void AppProgramSettings_CommandExecuted()
        {
            try
            {
                // Initialize view model for editing settings
                ConfigViewModel dlgVm = new ConfigViewModel();
                dlgVm.LoadOptionsFromModel(_SettingsManager.SettingData);

                // Create dialog and attach viewmodel to view datacontext
                Window dlg = ViewSelector.GetDialogView(dlgVm, Application.Current.MainWindow);

                dlg.ShowDialog();

	            if (dlgVm.WindowCloseResult != true) return;
	            dlgVm.SaveOptionsToModel(_SettingsManager.SettingData);

	            if (_SettingsManager.SettingData.IsDirty)
		            _SettingsManager.SaveOptions(_AppCore.DirFileAppSettingsData, _SettingsManager.SettingData);
            }
            catch (Exception exp)
            {
                Logger.Error(exp.Message, exp);
                _MsgBox.Show(exp, Util.Local.Strings.STR_MSG_UnknownError_Caption,
                             MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
                             _AppCore.IssueTrackerLink,
                             _AppCore.IssueTrackerLink,
                             Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
            }
        }

        #region Application_About_Command
        private void AppAbout_CommandExecuted()
        {
            try
            {
                var vm = new AboutViewModel();
                Window dlg = ViewSelector.GetDialogView(vm, Application.Current.MainWindow);

                dlg.ShowDialog();
            }
            catch (Exception exp)
            {
                Logger.Error(exp.Message, exp);
                _MsgBox.Show(exp, Util.Local.Strings.STR_MSG_UnknownError_Caption,
                             MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
                             _AppCore.IssueTrackerLink,
                             _AppCore.IssueTrackerLink,
                             Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
            }
        }
        #endregion Application_About_Command

        #region Recent File List Pin Unpin Commands
        private void PinCommand_Executed(object o, ExecutedRoutedEventArgs e)
        {
            try
            {
                var cmdParam = o as IMRUEntryViewModel;

                if (cmdParam == null)
                    return;

                if (e != null)
                    e.Handled = true;

                bool isPinnedParam = cmdParam.IsPinned == 0;    // Pin this if it was not pinned before or

                GetToolWindowVm<RecentFilesTWViewModel>().MruList.PinUnpinEntry(isPinnedParam, cmdParam);
            }
            catch (Exception exp)
            {
                Logger.Error(exp.Message, exp);
                _MsgBox.Show(exp, Util.Local.Strings.STR_MSG_UnknownError_Caption,
                             MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
                             _AppCore.IssueTrackerLink,
                             _AppCore.IssueTrackerLink,
                             Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
            }
        }

        private void AddMRUEntry_Executed(object o, ExecutedRoutedEventArgs e)
        {
            try
            {
                var cmdParam = o as IMRUEntryViewModel;

                if (cmdParam == null)
                    return;

                if (e != null)
                    e.Handled = true;

                GetToolWindowVm<RecentFilesTWViewModel>().MruList.UpdateEntry(cmdParam);
            }
            catch (Exception exp)
            {
                Logger.Error(exp.Message, exp);
                _MsgBox.Show(exp, Util.Local.Strings.STR_MSG_UnknownError_Caption,
                             MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
                             _AppCore.IssueTrackerLink,
                             _AppCore.IssueTrackerLink,
                             Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
            }
        }

        private void RemoveMRUEntry_Executed(object o, ExecutedRoutedEventArgs e)
        {
            try
            {
                var cmdParam = o as IMRUEntryViewModel;

                if (cmdParam == null)
                    return;

                if (e != null)
                    e.Handled = true;

                this.GetToolWindowVm<RecentFilesTWViewModel>().MruList.RemoveEntry(cmdParam);
            }
            catch (Exception exp)
            {
                Logger.Error(exp.Message, exp);
                _MsgBox.Show(exp, Util.Local.Strings.STR_MSG_UnknownError_Caption,
                             MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
                             _AppCore.IssueTrackerLink,
                             _AppCore.IssueTrackerLink,
                             Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
            }
        }
        #endregion Recent File List Pin Unpin Commands

        #region RequestClose [event]
        /// <summary>
        /// Method to be executed when user (or program) tries to close the application
        /// </summary>
        public void OnRequestClose()
        {
            try
            {
	            if (_ShutDownInProgress) return;
	            if (DialogCloseResult == null)
		            DialogCloseResult = true;      // Execute Close event via attached property

	            if (_ShutDownInProgressCancel)
	            {
		            _ShutDownInProgress = false;
		            _ShutDownInProgressCancel = false;
		            DialogCloseResult = null;
	            }
	            else
	            {
		            _ShutDownInProgress = true;

		            CommandManager.InvalidateRequerySuggested();

		            RequestClose?.Invoke(this, EventArgs.Empty);
	            }
            }
            catch (Exception exp)
            {
                _ShutDownInProgress = false;

                Logger.Error(exp.Message, exp);
                _MsgBox.Show(exp, Util.Local.Strings.STR_MSG_UnknownError_Caption,
                             MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
                             _AppCore.IssueTrackerLink,
                             _AppCore.IssueTrackerLink,
                             Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
            }
        }
        #endregion // RequestClose [event]

        private void SetActiveFileBaseDocument(IFileBaseViewModel vm)
        {
            try
            {
                ActiveDocument = vm;
            }
            catch (Exception exp)
            {
                Logger.Error(exp.Message, exp);
                _MsgBox.Show(exp, Util.Local.Strings.STR_MSG_UnknownError_Caption,
                             MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
                             _AppCore.IssueTrackerLink,
                             _AppCore.IssueTrackerLink,
                             Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
            }
        }

        /// <summary>
        /// Reset file view options in accordance with current program settings
        /// whenever a new file is internally created (on File Open or New File)
        /// </summary>
        /// <param name="vm"></param>
        private void SetActiveDocumentOnNewFileOrOpenFile(IDocumentEdi vm)
        {
            try
            {
                // Set scale factor in default size of text font
                vm.InitScaleView(_SettingsManager.SettingData.DocumentZoomUnit,
                                                 _SettingsManager.SettingData.DocumentZoomView);

                ActiveDocument = vm;
            }
            catch (Exception exp)
            {
                Logger.Error(exp.Message, exp);
                _MsgBox.Show(exp, Util.Local.Strings.STR_MSG_UnknownError_Caption,
                             MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
                             _AppCore.IssueTrackerLink,
                             _AppCore.IssueTrackerLink,
                             Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
            }
        }

        /// <summary>
        /// Implement part of requirement § 3.1.0 
        /// 
        /// The Open/SaveAs file dialog opens in the location of the currently active document (if any).
        /// 
        /// Otherwise, if there is no active document or the active document has never been saved before,
        /// the location of the last file open or file save/save as (which ever was last)
        /// is displayed in the Open/SaveAs File dialog.
        /// 
        /// The Open/SaveAs file dialog opens in the MyDocuments Windows user folder
        /// if none of the above conditions are true. (eg.: Open file for the very first
        /// time or last location does not exist).
        /// 
        /// The Open/Save/SaveAs file dialog opens in "C:\" if none of the above requirements
        /// can be implemented (eg.: MyDocuments folder does not exist or user has no access).
        /// 
        /// The last Open/Save/SaveAs file location used is stored and recovered between user sessions.
        /// </summary>
        /// <returns></returns>
        private string GetDefaultPath()
        {
            string sPath = string.Empty;

            try
            {
                // Generate a default path from cuurently or last active document
                if (ActiveEdiDocument != null)
                    sPath = ActiveEdiDocument.GetFilePath();

                if (sPath == string.Empty)
                    sPath = _SettingsManager.SessionData.GetLastActivePath();

                if (sPath == string.Empty)
                    sPath = _AppCore.MyDocumentsUserDir;
                else
                {
                    try
                    {
                        if (Directory.Exists(sPath) == false)
                            sPath = _AppCore.MyDocumentsUserDir;
                    }
                    catch
                    {
                        sPath = _AppCore.MyDocumentsUserDir;
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp.Message, exp);
                _MsgBox.Show(exp, Util.Local.Strings.STR_MSG_UnknownError_Caption,
                             MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
                             _AppCore.IssueTrackerLink,
                             _AppCore.IssueTrackerLink,
                             Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
            }

            return sPath;
        }

        /// <summary>
        /// Attempt to save data in file when
        /// File>Save As... or File>Save command
        /// is executed.
        /// </summary>
        /// <param name="doc"></param>
        /// <param name="saveAsFlag"></param>
        /// <returns></returns>
        internal bool OnSave(IFileBaseViewModel doc, bool saveAsFlag = false)
        {
            if (doc == null)
                return false;

	        if (!doc.CanSaveData)
		        throw new NotSupportedException(
			        doc.ToString());
	        var defaultFilter = GetDefaultFileFilter(doc, _DocumentTypeManager);

	        return OnSaveDocumentFile(doc, saveAsFlag, defaultFilter);

        }

	    /// <summary>
	    /// Returns the default file extension filter strings
	    /// that can be used for each corresponding document
	    /// type (viewmodel), or an empty string if no document
	    /// type (viewmodel) was matched.
	    /// </summary>
	    /// <param name="f"></param>
	    /// <param name="docManager"></param>
	    /// <returns></returns>
	    internal static string GetDefaultFileFilter(IFileBaseViewModel f, IDocumentTypeManager docManager)
        {
            if (f == null)
                return string.Empty;

            var filefilter = docManager.GetFileFilterEntries(f.DocumentTypeKey);

            return filefilter != null ? filefilter.GetFilterString() : string.Empty;
        }

        internal bool OnSaveDocumentFile(IFileBaseViewModel fileToSave,
                                         bool saveAsFlag = false,
                                         string fileExtensionFilter = "")
        {
            string filePath = (fileToSave == null ? string.Empty : fileToSave.FilePath);

            // Offer SaveAs file dialog if file has never been saved before (was created with new command)
            if (fileToSave != null)
                saveAsFlag = saveAsFlag | !fileToSave.IsFilePathReal;

            try
            {
                if (filePath == string.Empty || saveAsFlag == true)   // Execute SaveAs function
                {
                    var dlg = new SaveFileDialog();

                    try
                    {
                        dlg.FileName = System.IO.Path.GetFileName(filePath);
                    }
                    catch
                    {
                    }

                    dlg.InitialDirectory = this.GetDefaultPath();

                    if (string.IsNullOrEmpty(fileExtensionFilter) == false)
                        dlg.Filter = fileExtensionFilter;

                    if (dlg.ShowDialog().GetValueOrDefault() == true)     // SaveAs file if user OK'ed it so
                    {
                        filePath = dlg.FileName;

                        fileToSave.SaveFile(filePath);
                    }
                    else
                        return false;
                }
                else                                                  // Execute Save function
                    fileToSave.SaveFile(filePath);

                this.GetToolWindowVm<RecentFilesTWViewModel>().AddNewEntryIntoMRU(filePath);

                return true;
            }
            catch (Exception Exp)
            {
                string sMsg = Edi.Util.Local.Strings.STR_MSG_ErrorSavingFile;

                if (filePath.Length > 0)
                    sMsg = string.Format(CultureInfo.CurrentCulture, Edi.Util.Local.Strings.STR_MSG_ErrorWhileSavingFileX, Exp.Message, filePath);
                else
                    sMsg = string.Format(CultureInfo.CurrentCulture, Edi.Util.Local.Strings.STR_MSG_ErrorWhileSavingAFile, Exp.Message);

                _MsgBox.Show(Exp, sMsg, Edi.Util.Local.Strings.STR_MSG_ErrorSavingFile, MsgBoxButtons.OK);
            }

            return false;
        }

        internal bool OnCloseSaveDirtyFile(IFileBaseViewModel fileToClose)
        {
            if (fileToClose.IsDirty &&
                    fileToClose.CanSaveData)
            {
                var res = _MsgBox.Show(string.Format(CultureInfo.CurrentCulture, Util.Local.Strings.STR_MSG_SaveChangesForFile, fileToClose.FileName),
                                       ApplicationTitle,
                                       MsgBoxButtons.YesNoCancel, MsgBoxImage.Question,
                                       MsgBoxResult.Yes, false,
                                       MsgBoxResult.Yes);

                if (res == MsgBoxResult.Cancel)
                    return false;

                if (res == MsgBoxResult.Yes)
                {
                    return OnSave(fileToClose);
                }
            }

            return true;
        }

	    /// <summary>
	    /// Close the currently active file and set the file with the lowest index as active document.
	    /// 
	    /// TODO: The last active document that was active before the document being closed should be activated next.
	    /// </summary>
	    /// <param name="doc"></param>
	    /// <returns></returns>
	    internal bool Close(IFileBaseViewModel doc)
        {
            try
            {
                {
                    if (OnCloseSaveDirtyFile(doc) == false)
                        return false;

                    doc.DocumentEvent -= ProcessDocumentEvent;
                    doc.ProcessingResultEvent -= Vm_ProcessingResultEvent;

                    if (doc is IDocumentEdi)
                    {
                        var ediDoc = doc as IDocumentEdi;

                        ediDoc.ProcessingResultEvent -= Vm_ProcessingResultEvent;
                    }

                    int idx = _Files.IndexOf(doc);

                    _Files.Remove(doc);
                    doc.Dispose();

                    if (Documents.Count > idx)
                        ActiveDocument = _Files[idx];
                    else
                        if (Documents.Count > 1 && Documents.Count == idx)
                        ActiveDocument = _Files[idx - 1];
                    else
                            if (Documents.Count == 0)
                        ActiveDocument = null;
                    else
                        ActiveDocument = _Files[0];

                    return true;
                }

                /*
					// This could be a StartPage, Log4Net, or UML file or any other (read-only) document type
					if (doc != null)
					{
						if (doc.IsDirty == true)
						{
							if (this.OnCloseSaveDirtyFile(doc) == false)
								return false;
						}

						mFiles.Remove(doc);

						if (this.Documents.Count == 0)
							this.ActiveDocument = null;
						else
							this.ActiveDocument = this.mFiles[0];

						return true;
					}
				*/
            }
            catch (Exception exp)
            {
                Logger.Error(exp.Message, exp);
                _MsgBox.Show(exp, Util.Local.Strings.STR_MSG_UnknownError_Caption,
                             MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
                             _AppCore.IssueTrackerLink,
                             _AppCore.IssueTrackerLink,
                             Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
            }

            // Throw an exception if this method does not know how the input document type is to be closed
            throw new NotSupportedException(doc.ToString());
        }

        /// <summary>
        /// This can be used to close the attached view via ViewModel
        /// 
        /// Source: http://stackoverflow.com/questions/501886/wpf-mvvm-newbie-how-should-the-viewmodel-close-the-form
        /// </summary>
        public bool? DialogCloseResult
        {
            get { return _DialogCloseResult; }

	        private set
            {
	            if (_DialogCloseResult == value) return;
	            _DialogCloseResult = value;
	            RaisePropertyChanged(() => DialogCloseResult);
            }
        }

        /// <summary>
        /// Get/set property to determine whether window is in maximized state or not.
        /// (this can be handy to determine when a resize grip should be shown or not)
        /// </summary>
        public bool? IsNotMaximized
        {
            get { return _IsNotMaximized; }

	        set
            {
	            if (_IsNotMaximized == value) return;
	            _IsNotMaximized = value;
	            RaisePropertyChanged(() => IsNotMaximized);
            }
        }

        /// <summary>
        /// Gets/sets whether the workspace area is optimized or not.
        /// The optimized workspace is distructive free and does not
        /// show optional stuff like toolbar and status bar.
        /// </summary>
        public bool IsWorkspaceAreaOptimized
        {
            get { return _IsWorkspaceAreaOptimized; }

	        set
            {
	            if (_IsWorkspaceAreaOptimized == value) return;
	            _IsWorkspaceAreaOptimized = value;
	            RaisePropertyChanged(() => IsWorkspaceAreaOptimized);
            }
        }

        /// <summary>
        /// Check if pre-requisites for closing application are available.
        /// Save session data on closing and cancel closing process if necessary.
        /// </summary>
        /// <returns>true if application is OK to proceed closing with closed, otherwise false.</returns>
        public bool Exit_CheckConditions(object sender)
        {
            if (_ShutDownInProgress)
                return true;

            try
            {
                if (_Files != null)               // Close all open files and make sure there are no unsaved edits
                {
	                // If there are any: Ask user if edits should be saved
	                foreach (var f in Files)
	                {
		                if (OnCloseSaveDirtyFile(f)) continue;
		                _ShutDownInProgress = false;
		                return false;               // Cancel shutdown process (return false) if user cancels saving edits
	                }
                }

                // Do layout serialization after saving/closing files
                // since changes implemented by shut-down process are otherwise lost
                try
                {
                    _AppCore.CreateAppDataFolder();
                    ////this.SerializeLayout(sender);            // Store the current layout for later retrieval
                }
	            catch
	            {
		            // ignored
	            }
            }
            catch (Exception exp)
            {
                Logger.Error(exp.Message, exp);
                _MsgBox.Show(exp, Util.Local.Strings.STR_MSG_UnknownError_Caption,
                             MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
                             _AppCore.IssueTrackerLink,
                             _AppCore.IssueTrackerLink,
                             Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
            }

            return true;
        }

        /// <summary>
        /// Set the active document to the file in <seealso cref="fileNamePath"/>
        /// if this is currently open.
        /// </summary>
        /// <param name="fileNamePath"></param>
        internal bool SetActiveDocument(string fileNamePath)
        {
            try
            {
                if (Files.Count >= 0)
                {
                    EdiViewModel fi = Documents.SingleOrDefault(f => f.FilePath == fileNamePath);

                    if (fi != null)
                    {
                        ActiveDocument = fi;
                        return true;
                    }
                }
            }
            catch (Exception exp)
            {
                Logger.Error(exp.Message, exp);
                _MsgBox.Show(exp, Util.Local.Strings.STR_MSG_UnknownError_Caption,
                             MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
                             _AppCore.IssueTrackerLink,
                             _AppCore.IssueTrackerLink,
                             Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
            }

            return false;
        }

        /// <summary>
        /// Construct and add a new <seealso cref="StartPageViewModel"/> to intenral
        /// list of documents, if none is already present, otherwise return already
        /// present <seealso cref="StartPageViewModel"/> from internal document collection.
        /// </summary>
        /// <param name="createNewViewModelIfNecessary"></param>
        /// <returns></returns>
        internal StartPageViewModel GetStartPage(bool createNewViewModelIfNecessary)
        {
            List<StartPageViewModel> l = _Files.OfType<StartPageViewModel>().ToList();

	        if (l.Count != 0) return l[0];
	        if (createNewViewModelIfNecessary == false)
		        return null;
	        var s = new StartPageViewModel(_MruVM);

	        s.DocumentEvent += ProcessDocumentEvent;

	        _Files.Add(s);

	        return s;

        }

        /// <summary>
        /// Close document via dedicated event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessDocumentEvent(object sender, FileBaseEvent e)
        {
            var f = sender as FileBaseViewModel;

            switch (e.TypeOfEvent)
            {
                case FileEventType.Unknown:
                    break;

                case FileEventType.CloseDocument:
                    if (f != null)
                        CloseDocument(f);
                    break;

                case FileEventType.AdjustCurrentPath:
                    if (f != null)
                    {
                        // Query for an explorer tool window and return it
                        var eplorerTw = GetToolWindowVm<IExplorer>();

	                    eplorerTw?.NavigateToFolder(f.GetAlternativePath());
                    }
                    break;
            }
        }

        private void CloseDocument(FileBaseViewModel f)
        {
            if (f != null)
            {
                // Detach EdiViewModel specific events
                if (f is EdiViewModel)
                {
                    EdiViewModel eVM = f as EdiViewModel;
                    eVM.ProcessingResultEvent -= Vm_ProcessingResultEvent;
                }

                this.Close(f);
            }
        }

        /// <summary>
        /// Handle Processing results from asynchronous tasks that are
        /// executed in a viewmodel and return later with a Result (eg.: Async load of document)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Vm_ProcessingResultEvent(object sender, ProcessResultEvent e)
        {
	        if (!(sender is IDocumentFileWatcher)) return;
	        IDocumentFileWatcher watcher = (IDocumentFileWatcher) sender;

	        try
	        {
		        // Activate file watcher for this document
		        watcher.EnableDocumentFileWatcher(true);
	        }
	        catch (Exception ex)
	        {
		        _MsgBox.Show(ex, "An unexpected error occured", MsgBoxButtons.OK, MsgBoxImage.Alert);
	        }

	        var vm = sender as EdiViewModel;

	        try
	        {
		        switch (e.TypeOfResult)
		        {
			        case TypeOfResult.FileLoad:      // Process an EdiViewModel file load event mResult
				        if (e.InnerException != null)
				        {
					        if (vm != null)
					        {
						        Exception error = vm.GetInnerMostException(e.InnerException);

						        string filePath = vm.FilePath;
						        CloseDocument(vm);
						        ////vm = null;

						        if (error != null && filePath != null)
						        {
							        if (error is FileNotFoundException)
							        {
								        if (_MruVM.FindEntry(filePath) == null) return;
								        if (_MsgBox.Show(string.Format(Util.Local.Strings.STR_ERROR_LOADING_FILE_MSG, filePath),
									            Util.Local.Strings.STR_ERROR_LOADING_FILE_CAPTION, MsgBoxButtons.YesNo) == MsgBoxResult.Yes)
								        {
                                            _MruVM.RemoveEntry(filePath);
								        }

								        return;
							        }
						        }
					        }

					        _MsgBox.Show(e.InnerException, "An unexpected error occured",
						        MsgBoxButtons.OK, MsgBoxImage.Alert);
				        }
				        break;

			        default:
				        throw new NotImplementedException(e.TypeOfResult.ToString());
		        }
	        }
	        catch (Exception exp)
	        {
		        Logger.Error(exp);

		        _MsgBox.Show(exp, "An unexpected error occured", MsgBoxButtons.OK, MsgBoxImage.Alert);
	        }
        }

        /// <summary>
        /// Helper method for viewmodel resolution for avalondock contentids
        /// and specific document viewmodels. Careful: the Start Page is also
        /// a document but cannot be loaded, saved, or edit as other documents can.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private IFileBaseViewModel ReloadDocument(string path)
        {
            IFileBaseViewModel ret = null;

	        if (string.IsNullOrWhiteSpace(path))
			{
				return null;
			}

			switch (path)
	        {
		        case StartPageViewModel.StartPageContentId: // Re-create start page content
			        if (GetStartPage(false) == null)
			        {
				        ret = GetStartPage(true);
			        }
			        break;

		        default:
			        if (path.Contains("<") && path.Contains(">"))
			        {
				        _MessageManager.Output.AppendLine(
					        $"Warning: Cannot resolve tool window or document page: '{path}'.");

				        _MessageManager.Output.AppendLine(
					        "Check the current program configuration to make that it is present.");

				        return null;
			        }

			        // Re-create Edi document (text file or log4net document) content
			        ret = Open(path, CloseDocOnError.WithoutUserNotification);
			        break;
	        }

	        return ret;
        }

        /// <summary>
        /// Return a typed viewmodel from a collection of tool window viewmodels.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private T GetToolWindowVm<T>() where T : class
        {
            if (_ToolRegistry == null)
                return null;

            // Query for a RecentFiles tool window and return it
            return _ToolRegistry.Tools.FirstOrDefault(d => d is T) as T;
        }

        /// <summary>
        /// Method executes when tool window registration publishers
        /// relay the fact that a new tool window has been registered.
        /// </summary>
        /// <param name="args"></param>
        private void OnPublisToolWindows(object sender, PublishToolWindowEventArgs args)
        {
	        // This particular event is needed since the build in RecentFiles
	        // property is otherwise without content since it may be queried
	        // for the menu entry - before the tool window is registered
	        if (args?.Tool is RecentFilesTWViewModel)
            {
                RaisePropertyChanged(() => RecentFiles);
            }
        }
        #endregion methods
    }
}