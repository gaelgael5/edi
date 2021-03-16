﻿namespace Edi.Apps.ViewModels
{
	using Enums;
	using Core;
	using Core.Interfaces;
	using Core.ViewModels;
	using Core.ViewModels.Command;
	using Documents.ViewModels.EdiDoc;
	using Documents.ViewModels.StartPage;
	using Themes;
	using Files.ViewModels.RecentFiles;
	using MiniUML.Framework;
	using MRULib.MRU.Enums;
	using MRULib.MRU.Interfaces;
	using MsgBox;
	using System;
	using System.Diagnostics;
	using System.Threading;
	using System.Windows;
	using System.Windows.Input;
	using System.Windows.Threading;

	public partial class ApplicationViewModel
	{

		private bool Closing_CanExecute()
		{
			return !_ShutDownInProgress;

			// Check if conditions within the WorkspaceViewModel are suitable to close the application
			// eg.: Prompt to Cancel long running background tasks such as Search - Replace in Files (if any)
		}

		/// <summary>
		/// Bind a window to some commands to be executed by the viewmodel.
		/// </summary>
		/// <param name="win"></param>
		public void InitCommandBinding(Window win)
		{
			InitEditCommandBinding(win);

			win.CommandBindings.Add(new CommandBinding(AppCommand.Exit,
			(s, e) =>
			{
				AppExit_CommandExecuted();
				e.Handled = true;
			}));

			win.CommandBindings.Add(new CommandBinding(AppCommand.About,
			(s, e) =>
			{
				AppAbout_CommandExecuted();
				e.Handled = true;
			}));

			win.CommandBindings.Add(new CommandBinding(AppCommand.ProgramSettings,
			(s, e) =>
			{
				AppProgramSettings_CommandExecuted();
				e.Handled = true;
			}));

            win.CommandBindings.Add(new CommandBinding(AppCommand.ShowToolWindow,
            (s, e) =>
            {
                if (e == null)
                    return;

                var toolwindowviewmodel = e.Parameter as IToolWindow;

                if (toolwindowviewmodel == null)
                    return;


                if (toolwindowviewmodel is IRegisterableToolWindow)
                {
                    IRegisterableToolWindow registerTW = toolwindowviewmodel as IRegisterableToolWindow;

                    registerTW.SetToolWindowVisibility(this, !toolwindowviewmodel.IsVisible);
                }
                else
                    toolwindowviewmodel.SetToolWindowVisibility(!toolwindowviewmodel.IsVisible);

                e.Handled = true;
            }));

            // Standard File New command binding via ApplicationCommands enumeration
            win.CommandBindings.Add(new CommandBinding(ApplicationCommands.New,
            (s, e) =>
            {
                TypeOfDocument t = TypeOfDocument.EdiTextEditor;

                if (e != null)
                {
                    e.Handled = true;

                    if (e.Parameter != null)
                    {
                        if (e.Parameter is TypeOfDocument)
                            t = (TypeOfDocument)e.Parameter;
                    }
                }

                this.OnNew(t);
            }
            ));

            // Standard File Open command binding via ApplicationCommands enumeration
            win.CommandBindings.Add(new CommandBinding(ApplicationCommands.Open,
			(s, e) =>
			{
				string t = string.Empty;

				if (e?.Parameter is string)
					t = (string)e.Parameter;

				OnOpen(t);
				if (e != null) e.Handled = true;
			}
			));

			// Close Document command
			// Closes the FileViewModel document supplied in e.parameter
			// or the Active document
			win.CommandBindings.Add(new CommandBinding(AppCommand.CloseFile,
			(s, e) =>
			{
				try
				{
					FileBaseViewModel f = null;

					if (e != null)
					{
						e.Handled = true;
						f = e.Parameter as FileBaseViewModel;
					}

					if (f != null)
						Close(f);
					else
					{
						if (ActiveDocument != null)
							Close(ActiveDocument);
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
			},
			(s, e) =>
			{
				try
				{
                    if (e != null)
                    {
                        e.Handled = true;
                        e.CanExecute = false;

                        EdiViewModel f = null;

                        if (e != null)
                        {
                            e.Handled = true;
                            f = e.Parameter as EdiViewModel;
                        }

                        if (f != null)
                            e.CanExecute = f.CanClose();
                        else
                        {
                            if (this.ActiveDocument != null)
                                e.CanExecute = this.ActiveDocument.CanClose();
                        }
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
			}));

			// Change the WPF/TextEditor highlighting theme currently used in the application
			win.CommandBindings.Add(new CommandBinding(AppCommand.ViewTheme,
            (s, e) => this.ChangeThemeCmd_Executed(s, e, win.Dispatcher)));

            win.CommandBindings.Add(new CommandBinding(AppCommand.BrowseUrl,
			(s, e) =>
			{
				Process.Start(new ProcessStartInfo("https://github.com/Dirkster99/Edi"));
			}));

			win.CommandBindings.Add(new CommandBinding(AppCommand.ShowStartPage,
			(s, e) =>
            {
                ShowStartPage();
            }));

			win.CommandBindings.Add(new CommandBinding(AppCommand.ToggleOptimizeWorkspace,
			(s, e) =>
			{
				Logger.InfoFormat("TRACE AppCommand.ToggleOptimizeWorkspace parameter is {0}.", e?.ToString() ?? "(null)");

				try
				{
					var newViewSetting = !IsWorkspaceAreaOptimized;
					IsWorkspaceAreaOptimized = newViewSetting;
				}
				catch (Exception exp)
				{
					Logger.Error(exp.Message, exp);
					_MsgBox.Show(exp, Util.Local.Strings.STR_MSG_IssueTrackerTitle, MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
								 _AppCore.IssueTrackerLink, _AppCore.IssueTrackerLink,
								 Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
				}
			}));

			win.CommandBindings.Add(new CommandBinding(AppCommand.LoadFile,
			(s, e) =>
			{
				try
				{
                    Logger.InfoFormat("TRACE AppCommand.LoadFile parameter is {0}.", (e == null ? "(null)" : e.ToString()));

                    if (e == null)
                        return;

                    string filename = e.Parameter as string;

                    if (filename == null)
                        return;

                    Logger.InfoFormat("TRACE AppCommand.LoadFile with: '{0}'", filename);

                    this.Open(filename);
				}
				catch (Exception exp)
				{
					Logger.Error(exp.Message, exp);
					_MsgBox.Show(exp, Util.Local.Strings.STR_MSG_IssueTrackerTitle, MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
								 _AppCore.IssueTrackerLink, _AppCore.IssueTrackerLink,
								 Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
				}
			}));

			win.CommandBindings.Add(new CommandBinding(ApplicationCommands.Save,
			(s, e) =>
			{
				try
				{
					if (e != null)
						e.Handled = true;

					if (ActiveDocument != null)
						OnSave(ActiveDocument);
				}
				catch (Exception exp)
				{
					Logger.Error(exp.Message, exp);
					_MsgBox.Show(exp, Util.Local.Strings.STR_MSG_UnknownError_Caption,
								 MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
								 _AppCore.IssueTrackerLink, _AppCore.IssueTrackerLink,
								 Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
				}
			},
			(s, e) =>
			{
				if (e == null) return;
				e.Handled = true;

				if (ActiveDocument != null)
					e.CanExecute = ActiveDocument.CanSave();
			}));

			win.CommandBindings.Add(new CommandBinding(ApplicationCommands.SaveAs,
			(s, e) =>
			{
				try
				{
					if (e != null)
						e.Handled = true;

					if (ActiveDocument == null)
                        return;

                    if (!OnSave(ActiveDocument, true))
                        return;

					_MruVM.UpdateEntry(ActiveDocument.FilePath);
					_SettingsManager.SessionData.LastActiveFile = ActiveDocument.FilePath;
				}
				catch (Exception exp)
				{
					Logger.Error(exp.Message, exp);
					_MsgBox.Show(exp, Util.Local.Strings.STR_MSG_IssueTrackerTitle, MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
								 _AppCore.IssueTrackerLink,
								 _AppCore.IssueTrackerLink,
								 Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
				}
			},
			(s, e) =>
			{
				try
				{
					if (e != null)
					{
						e.Handled = true;
						e.CanExecute = false;

						if (ActiveDocument != null)
							e.CanExecute = ActiveDocument.CanSaveAs();
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
			));

			// Execute a command to save all edited files and current program settings
			win.CommandBindings.Add(new CommandBinding(AppCommand.SaveAll,
			(s, e) =>
			{
				try
				{
					// Save all edited documents
					if (_Files != null)               // Close all open files and make sure there are no unsaved edits
					{                                     // If there are any: Ask user if edits should be saved
						IFileBaseViewModel activeDoc = ActiveDocument;

						try
						{
							foreach (var f in Files)
							{
								if (f == null) continue;
								if (!f.IsDirty || !f.CanSaveData) continue;
								ActiveDocument = f;
								OnSave(f);
							}
						}
						catch (Exception exp)
						{
							_MsgBox.Show(exp.ToString(), Util.Local.Strings.STR_MSG_UnknownError_Caption, MsgBoxButtons.OK);
						}
						finally
						{
							if (activeDoc != null)
								ActiveDocument = activeDoc;
						}
					}

					// Save program settings
					SaveConfigOnAppClosed();
				}
				catch (Exception exp)
				{
					Logger.Error(exp.Message, exp);
					_MsgBox.Show(exp, Util.Local.Strings.STR_MSG_IssueTrackerTitle, MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
								 _AppCore.IssueTrackerLink,
								 _AppCore.IssueTrackerLink,
								 Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
				}
			}));

			// Execute a command to export UML editor content as image
			win.CommandBindings.Add(new CommandBinding(AppCommand.ExportUmlToImage,
			(s, e) =>
			{
				try
				{
					if (vm_DocumentViewModel?.dm_DocumentDataModel.State == DataModel.ModelState.Ready)
					{
						vm_DocumentViewModel.ExecuteExport(s, e, ActiveDocument.FileName + ".png");
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
			},
			(s, e) =>  // Execute this command only if an UML document is currently active
			{
				if (vm_DocumentViewModel != null)
					e.CanExecute = (vm_DocumentViewModel.dm_DocumentDataModel.State == DataModel.ModelState.Ready);
				else
					e.CanExecute = false;
			}
			));

			// Execute a command to export Text editor content as highlighted image content
			win.CommandBindings.Add(new CommandBinding(AppCommand.ExportTextToHtml,
			(s, e) =>
			{
				try
				{
					ActiveEdiDocument?.ExportToHtml(ActiveDocument.FileName + ".html",
						_SettingsManager.SettingData.TextToHTML_ShowLineNumbers,
						_SettingsManager.SettingData.TextToHTML_AlternateLineBackground);
				}
				catch (Exception exp)
				{
					Logger.Error(exp.Message, exp);
					_MsgBox.Show(exp, Util.Local.Strings.STR_MSG_IssueTrackerTitle, MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
								 _AppCore.IssueTrackerLink,
								 _AppCore.IssueTrackerLink,
								 Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
				}
			},
			(s, e) =>  // Execute this command only if a Text document is currently active
			{
				e.CanExecute = ActiveEdiDocument != null;
			}
			));


			// Removes ALL MRU entries (even pinned entries) from the current list of entries.
			win.CommandBindings.Add(new CommandBinding(AppCommand.ClearAllMruItemsCommand,
			(s, e) =>
			{
				GetToolWindowVm<RecentFilesTWViewModel>().MruList.Clear();
			}));

			// <summary>
			/// Gets a command that removes all items that are older
			/// than a given <see cref="GroupType"/>.
			// Eg.: Remove all MRU entries older than yesterday.
			// </summary>
			win.CommandBindings.Add(new CommandBinding(AppCommand.RemoveItemsOlderThanThisCommand,
			(s, e) =>
			{
				if (e.Parameter is GroupType == false)
					return;

				var param = (GroupType)e.Parameter;

				GetToolWindowVm<RecentFilesTWViewModel>().MruList.RemoveEntryOlderThanThis(param);
			},
			(s, e) =>
			{
				if (e.Parameter is GroupType == false)
				{
					e.CanExecute = false;
					return;
				}

				e.CanExecute = true;
			}));

			win.CommandBindings.Add(new CommandBinding(AppCommand.MovePinnedMruItemUpCommand,
			(s, e) =>
			{
				if (e.Parameter is IMRUEntryViewModel == false)
					return;

				var param = (IMRUEntryViewModel)e.Parameter;

				GetToolWindowVm<RecentFilesTWViewModel>().MruList.MovePinnedEntry(MoveMRUItem.Up, param);
			},
			(s, e) =>
			{
				if (e.Parameter is IMRUEntryViewModel == false)
				{
					e.CanExecute = false;
					return;
				}

				if (((IMRUEntryViewModel)e.Parameter).IsPinned == 0)  //Make sure it is pinned
				{
					e.CanExecute = false;
					return;
				}

				e.CanExecute = true;
			}));

			win.CommandBindings.Add(new CommandBinding(AppCommand.MovePinnedMruItemDownCommand,
			(s, e) =>
			{
				if (e.Parameter is IMRUEntryViewModel == false)
					return;

				var param = (IMRUEntryViewModel)e.Parameter;

				GetToolWindowVm<RecentFilesTWViewModel>().MruList.MovePinnedEntry(MoveMRUItem.Down, param);
			},
			(s, e) =>
			{
				if (e.Parameter is IMRUEntryViewModel == false)
				{
					e.CanExecute = false;
					return;
				}

				if (((IMRUEntryViewModel)e.Parameter).IsPinned == 0)  //Make sure it is pinned
				{
					e.CanExecute = false;
					return;
				}

				e.CanExecute = true;
			}));

			win.CommandBindings.Add(new CommandBinding(AppCommand.PinItemCommand,
			(s, e) =>
			{
				GetToolWindowVm<RecentFilesTWViewModel>().MruList.PinUnpinEntry(true, e.Parameter as IMRUEntryViewModel);
			},
			(s, e) =>
			{
				if (e.Parameter is IMRUEntryViewModel == false)
				{
					e.CanExecute = false;
					return;
				}

				if (((IMRUEntryViewModel)e.Parameter).IsPinned == 0)  //Make sure it is pinned
				{
					e.CanExecute = true;
					return;
				}

				e.CanExecute = false;
			}));

			win.CommandBindings.Add(new CommandBinding(AppCommand.UnPinItemCommand,
			(s, e) =>
			{
				if (e.Parameter is IMRUEntryViewModel == false)
					return;

				GetToolWindowVm<RecentFilesTWViewModel>().MruList.PinUnpinEntry(false, (IMRUEntryViewModel) e.Parameter);
			},
			(s, e) =>
			{
				if (e.Parameter is IMRUEntryViewModel == false)
				{
					e.CanExecute = false;
					return;
				}

				if (((IMRUEntryViewModel)e.Parameter).IsPinned == 0)  //Make sure it is pinned
				{
					e.CanExecute = false;
					return;
				}

				e.CanExecute = true;
			}));

			win.CommandBindings.Add(new CommandBinding(AppCommand.PinUnpin,
			(s, e) =>
			{
				PinCommand_Executed(e.Parameter, e);
			}));

			win.CommandBindings.Add(new CommandBinding(AppCommand.RemoveMruEntry,
			(s, e) =>
			{
				RemoveMRUEntry_Executed(e.Parameter, e);
			}));

			win.CommandBindings.Add(new CommandBinding(AppCommand.AddMruEntry,
			(s, e) =>
			{
				AddMRUEntry_Executed(e.Parameter, e);
			}));
		}

        public void ShowStartPage()
        {
            StartPageViewModel spage = GetStartPage(true);

            if (spage == null) return;
            Logger.InfoFormat("TRACE Before setting startpage as ActiveDocument");
            ActiveDocument = spage;
            Logger.InfoFormat("TRACE After setting startpage as ActiveDocument");
        }

        /// <summary>
        /// This procedure changes the current WPF Application Theme into another theme
        /// while the application is running (re-boot should not be required).
        /// </summary>
        /// <param name="e"></param>
        /// <param name="disp"></param>
        private void ChangeThemeCmd_Executed(object s,
                                            ExecutedRoutedEventArgs e,
                                            System.Windows.Threading.Dispatcher disp)
        {
            string oldTheme = ApplicationThemes.DefaultThemeName;

            try
            {
                if (e == null)
                    return;

                if (e.Parameter == null)
                    return;

                string newThemeName = e.Parameter as string;

                // Check if request is available
                if (newThemeName == null)
                    return;

                oldTheme = _SettingsManager.SettingData.CurrentTheme;

                // The Work to perform on another thread
                ThreadStart start = delegate
                {
                    // This works in the UI tread using the dispatcher with highest Priority
                    disp.Invoke(DispatcherPriority.Send,
                    (Action)(() =>
                    {
                        try
                        {
                            if (ApplicationThemes.SetSelectedTheme(newThemeName) == false)
                                return;

                            _SettingsManager.SettingData.CurrentTheme = newThemeName;
                            ResetTheme();                        // Initialize theme in process
                        }
                        catch (Exception exp)
                        {
                            Logger.Error(exp.Message, exp);
                            _MsgBox.Show(exp, Edi.Util.Local.Strings.STR_MSG_IssueTrackerTitle, MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
                                         _AppCore.IssueTrackerLink,
                                         _AppCore.IssueTrackerLink,
                                         Edi.Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
                        }
                    }));
                };

                // Create the thread and kick it started!
                Thread thread = new Thread(start);

                thread.Start();
            }
            catch (Exception exp)
            {
                _SettingsManager.SettingData.CurrentTheme = oldTheme;

                Logger.Error(exp.Message, exp);
                _MsgBox.Show(exp, Edi.Util.Local.Strings.STR_MSG_IssueTrackerTitle, MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
                             _AppCore.IssueTrackerLink,
                             _AppCore.IssueTrackerLink,
                             Edi.Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
            }
        }

        #region EditorCommands

        /// <summary>
        /// Set command bindings necessary to perform copy/cut/paste operations
        /// </summary>
        /// <param name="win"></param>
        public void InitEditCommandBinding(Window win)
		{
			win.CommandBindings.Add(new CommandBinding(AppCommand.DisableHighlighting,    // Select all text in a document
			(s, e) =>
			{
				try
				{
					if (!(ActiveDocument is EdiViewModel)) return;
					EdiViewModel f = (EdiViewModel)ActiveDocument;
					f.DisableHighlighting();
				}
				catch (Exception exp)
				{
					Logger.Error(exp.Message, exp);
					_MsgBox.Show(exp, Util.Local.Strings.STR_MSG_IssueTrackerTitle, MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
								 _AppCore.IssueTrackerLink,
								 _AppCore.IssueTrackerLink,
								 Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
				}
			},
			(s, e) =>
			{
				EdiViewModel f = ActiveDocument as EdiViewModel;

				if (f?.HighlightingDefinition != null)
				{
					e.CanExecute = true;
					return;
				}

				e.CanExecute = false;
			}));

			#region GotoLine FindReplace
			win.CommandBindings.Add(new CommandBinding(AppCommand.GotoLine,    // Goto line n in a document
			(s, e) =>
			{
				try
				{
					e.Handled = true;

					ShowGotoLineDialog();
				}
				catch (Exception exp)
				{
					Logger.Error(exp.Message, exp);
					_MsgBox.Show(exp, Util.Local.Strings.STR_MSG_IssueTrackerTitle, MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
								 _AppCore.IssueTrackerLink,
								 _AppCore.IssueTrackerLink,
								 Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
				}
			},
			(s, e) => { e.CanExecute = CanExecuteIfActiveDocumentIsEdiViewModel(); }));

			win.CommandBindings.Add(new CommandBinding(AppCommand.FindText,    // Find text in a document
			(s, e) =>
			{
				try
				{
					e.Handled = true;

					ShowFindReplaceDialog();
				}
				catch (Exception exp)
				{
					Logger.Error(exp.Message, exp);
					_MsgBox.Show(exp, Util.Local.Strings.STR_MSG_IssueTrackerTitle, MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
								 _AppCore.IssueTrackerLink,
								 _AppCore.IssueTrackerLink,
								 Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
				}
			},
			(s, e) => { e.CanExecute = CanExecuteIfActiveDocumentIsEdiViewModel(); }));

			win.CommandBindings.Add(new CommandBinding(AppCommand.FindPreviousText,    // Find text in a document
			(s, e) =>
			{
				try
				{
					e.Handled = true;


					if (!(ActiveDocument is EdiViewModel)) return;
					if (FindReplaceVm != null)
					{
						FindReplaceVm.FindNext(FindReplaceVm, true);
					}
					else
					{
						ShowFindReplaceDialog();
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
			},
			(s, e) => { e.CanExecute = CanExecuteIfActiveDocumentIsEdiViewModel(); }));

			win.CommandBindings.Add(new CommandBinding(AppCommand.FindNextText,    // Find text in a document
			(s, e) =>
			{
				try
				{
					e.Handled = true;


					if (!(ActiveDocument is EdiViewModel)) return;

					if (FindReplaceVm != null)
					{
						FindReplaceVm.FindNext(FindReplaceVm, false);
					}
					else
					{
						ShowFindReplaceDialog();
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
			},
			(s, e) => { e.CanExecute = CanExecuteIfActiveDocumentIsEdiViewModel(); }));

			win.CommandBindings.Add(new CommandBinding(AppCommand.ReplaceText, // Find and replace text in a document
			(s, e) =>
			{
				try
				{
					e.Handled = true;

					ShowFindReplaceDialog(false);
				}
				catch (Exception exp)
				{
					Logger.Error(exp.Message, exp);
					_MsgBox.Show(exp, Util.Local.Strings.STR_MSG_IssueTrackerTitle, MsgBoxButtons.OK, MsgBoxImage.Error, MsgBoxResult.NoDefaultButton,
								 _AppCore.IssueTrackerLink,
								 _AppCore.IssueTrackerLink,
								 Util.Local.Strings.STR_MSG_IssueTrackerText, null, true);
				}
			},
			(s, e) => { e.CanExecute = CanExecuteIfActiveDocumentIsEdiViewModel(); }));
			#endregion GotoLine FindReplace
		}

		#region ToggleEditorOptionCommand
		RelayCommand<ToggleEditorOption> _toggleEditorOptionCommand;
		public ICommand ToggleEditorOptionCommand
		{
			get
			{
				return _toggleEditorOptionCommand ?? (_toggleEditorOptionCommand = new RelayCommand<ToggleEditorOption>
					   ((p) => OnToggleEditorOption(p),
						   (p) => CanExecuteIfActiveDocumentIsEdiViewModel()));
			}
		}

		private void OnToggleEditorOption(object parameter)
		{
            EdiViewModel f = this.ActiveDocument as EdiViewModel;

            if (f == null)
                return;

            if (parameter == null)
                return;

            if ((parameter is ToggleEditorOption) == false)
                return;

            ToggleEditorOption t = (ToggleEditorOption)parameter;

            if (f != null)
            {
                switch (t)
                {
                    case ToggleEditorOption.WordWrap:
                        f.WordWrap = !f.WordWrap;
                        break;

                    case ToggleEditorOption.ShowLineNumber:
                        f.ShowLineNumbers = !f.ShowLineNumbers;
                        break;

                    case ToggleEditorOption.ShowSpaces:
                        f.TextOptions.ShowSpaces = !f.TextOptions.ShowSpaces;
                        break;

                    case ToggleEditorOption.ShowTabs:
                        f.TextOptions.ShowTabs = !f.TextOptions.ShowTabs;
                        break;

                    case ToggleEditorOption.ShowEndOfLine:
                        f.TextOptions.ShowEndOfLine = !f.TextOptions.ShowEndOfLine;
                        break;

                    default:
                        break;
                }
            }
        }
        #endregion ToggleEditorOptionCommand

        private bool CanExecuteIfActiveDocumentIsEdiViewModel()
		{

			if (ActiveDocument is EdiViewModel)
			{
				//EdiViewModel f = this.ActiveDocument as EdiViewModel;
				return true;
			}

			return false;
		}
		#endregion EditorCommands
	}
}