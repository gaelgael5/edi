namespace MWindowLib.Behaviours
{
    using Controls;
	using global::Microsoft.Xaml.Behaviors;
	using Native;
    using System;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interop;
    using System.Windows.Shell;

    /// <summary>
    /// This class supports custom window styles while removing the normal
    /// Windows version dependent border of a Window.
    /// http://mahapps.com/guides/advanced-guide.html
    /// </summary>
    public class BorderlessWindowBehavior : Behavior<Window>
    {
        private IntPtr handle;
        private HwndSource hwndSource;
        private WindowChrome windowChrome;
        private PropertyChangeNotifier borderThicknessChangeNotifier;
        private Thickness? savedBorderThickness;
        private Thickness? savedResizeBorderThickness;
        private PropertyChangeNotifier topMostChangeNotifier;
        private bool savedTopMost;

        // Versions can be taken from https://msdn.microsoft.com/library/windows/desktop/ms724832.aspx
        ////private bool isWindwos10OrHigher;

        ////private static bool IsWindows10OrHigher()
        ////{
        ////    // Snippet from Koopakiller https://dotnet-snippets.de/snippet/os-version-name-mit-wmi/4929
        ////    using (var mos = new ManagementObjectSearcher("SELECT Caption, Version FROM Win32_OperatingSystem"))
        ////    {
        ////        var attribs = mos.Get().OfType<ManagementObject>();
        ////        //caption = attribs.FirstOrDefault().GetPropertyValue("Caption").ToString() ?? "Unknown";
        ////        var version = new Version((attribs.FirstOrDefault()?.GetPropertyValue("Version") ?? "0.0.0.0").ToString());
        ////        return version >= new Version(10, 0);
        ////    }
        ////}

        /// <summary>
        /// Initializes a new instance of the System.Windows.Interactivity.Behavior class.
        ///
        /// Hooks any necessary handlers to the AssociatedObject.
        /// </summary>
        protected override void OnAttached()
        {
            ////this.isWindwos10OrHigher = IsWindows10OrHigher();

            this.windowChrome = new WindowChrome
            {
                ////#if NET4_5
                ResizeBorderThickness = SystemParameters.WindowResizeBorderThickness,
                ////#else
                ////                ResizeBorderThickness = SystemParameters2.Current.WindowResizeBorderThickness,
                ////#endif
                CaptionHeight = 0,
                CornerRadius = new CornerRadius(0),
                GlassFrameThickness = new Thickness(0),
                UseAeroCaptionButtons = false
            };

            ////var metroWindow = this.AssociatedObject as MetroWindow;
            ////if (metroWindow != null)
            ////{
            ////    this.windowChrome.IgnoreTaskbarOnMaximize = metroWindow.IgnoreTaskbarOnMaximize;
            ////    this.windowChrome.UseNoneWindowStyle = metroWindow.UseNoneWindowStyle;
            ////    System.ComponentModel.DependencyPropertyDescriptor.FromProperty(MetroWindow.IgnoreTaskbarOnMaximizeProperty, typeof(MetroWindow))
            ////          .AddValueChanged(this.AssociatedObject, this.IgnoreTaskbarOnMaximizePropertyChangedCallback);
            ////    System.ComponentModel.DependencyPropertyDescriptor.FromProperty(MetroWindow.UseNoneWindowStyleProperty, typeof(MetroWindow))
            ////          .AddValueChanged(this.AssociatedObject, this.UseNoneWindowStylePropertyChangedCallback);
            ////}

            this.AssociatedObject.SetValue(WindowChrome.WindowChromeProperty, this.windowChrome);

            // no transparany, because it hase more then one unwanted issues
            var windowHandle = new WindowInteropHelper(this.AssociatedObject).Handle;
            if (!this.AssociatedObject.IsLoaded && windowHandle == IntPtr.Zero)
            {
                try
                {
                    this.AssociatedObject.AllowsTransparency = false;
                }
                catch (Exception)
                {
                    //For some reason, we can't determine if the window has loaded or not, so we swallow the exception.
                }
            }
            this.AssociatedObject.WindowStyle = WindowStyle.None;

            this.savedBorderThickness = this.AssociatedObject.BorderThickness;
            this.savedResizeBorderThickness = this.windowChrome.ResizeBorderThickness;
            this.borderThicknessChangeNotifier = new PropertyChangeNotifier(this.AssociatedObject, Control.BorderThicknessProperty);
            this.borderThicknessChangeNotifier.ValueChanged += this.BorderThicknessChangeNotifierOnValueChanged;

            this.savedTopMost = this.AssociatedObject.Topmost;
            this.topMostChangeNotifier = new PropertyChangeNotifier(this.AssociatedObject, Window.TopmostProperty);
            this.topMostChangeNotifier.ValueChanged += this.TopMostChangeNotifierOnValueChanged;

            // #1823 try to fix another nasty issue
            // WindowState = Maximized
            // ResizeMode = NoResize
            if (this.AssociatedObject.ResizeMode == ResizeMode.NoResize)
            {
                this.windowChrome.ResizeBorderThickness = new Thickness(0);
            }

            this.AssociatedObject.Loaded += this.AssociatedObject_Loaded;
            this.AssociatedObject.Unloaded += this.AssociatedObject_Unloaded;
            this.AssociatedObject.SourceInitialized += this.AssociatedObject_SourceInitialized;
            this.AssociatedObject.StateChanged += this.OnAssociatedObjectHandleMaximize;

            base.OnAttached();
        }

        private void BorderThicknessChangeNotifierOnValueChanged(object sender, EventArgs e)
        {
            this.savedBorderThickness = this.AssociatedObject.BorderThickness;
        }

        private void TopMostChangeNotifierOnValueChanged(object sender, EventArgs e)
        {
            this.savedTopMost = this.AssociatedObject.Topmost;
        }

        ////private void UseNoneWindowStylePropertyChangedCallback(object sender, EventArgs e)
        ////{
        ////    var metroWindow = sender as MetroWindow;
        ////    if (metroWindow != null && this.windowChrome != null)
        ////    {
        ////        if (!Equals(this.windowChrome.UseNoneWindowStyle, metroWindow.UseNoneWindowStyle))
        ////        {
        ////            this.windowChrome.UseNoneWindowStyle = metroWindow.UseNoneWindowStyle;
        ////            this.ForceRedrawWindowFromPropertyChanged();
        ////        }
        ////    }
        ////}
        ////
        ////private void IgnoreTaskbarOnMaximizePropertyChangedCallback(object sender, EventArgs e)
        ////{
        ////    var metroWindow = sender as MetroWindow;
        ////    if (metroWindow != null && this.windowChrome != null)
        ////    {
        ////        if (!Equals(this.windowChrome.IgnoreTaskbarOnMaximize, metroWindow.IgnoreTaskbarOnMaximize))
        ////        {
        ////            // another special hack to avoid nasty resizing
        ////            // repro
        ////            // ResizeMode="NoResize"
        ////            // WindowState="Maximized"
        ////            // IgnoreTaskbarOnMaximize="True"
        ////            // this only happens if we change this at runtime
        ////            var removed = this._ModifyStyle(Standard.WS.MAXIMIZEBOX | Standard.WS.MINIMIZEBOX | Standard.WS.THICKFRAME, 0);
        ////            this.windowChrome.IgnoreTaskbarOnMaximize = metroWindow.IgnoreTaskbarOnMaximize;
        ////            if (removed)
        ////            {
        ////                this._ModifyStyle(0, Standard.WS.MAXIMIZEBOX | Standard.WS.MINIMIZEBOX | Standard.WS.THICKFRAME);
        ////            }
        ////            this.ForceRedrawWindowFromPropertyChanged();
        ////        }
        ////    }
        ////}

        //// <summary>Add and remove a native WindowStyle from the HWND.</summary>
        //// <param name="removeStyle">The styles to be removed.  These can be bitwise combined.</param>
        //// <param name="addStyle">The styles to be added.  These can be bitwise combined.</param>
        //// <returns>Whether the styles of the HWND were modified as a result of this call.</returns>
        //// <SecurityNote>
        ////   Critical : Calls critical methods
        //// </SecurityNote>
        ////[SecurityCritical]
        ////private bool _ModifyStyle(WS removeStyle, WS addStyle)
        ////{
        ////    if (this.handle == IntPtr.Zero)
        ////    {
        ////        return false;
        ////    }
        ////    var intPtr = NativeMethods.GetWindowLongPtr(this.handle, GWL.STYLE);
        ////    var dwStyle = (WS)(Environment.Is64BitProcess ? intPtr.ToInt64() : intPtr.ToInt32());
        ////    var dwNewStyle = (dwStyle & ~removeStyle) | addStyle;
        ////    if (dwStyle == dwNewStyle)
        ////    {
        ////        return false;
        ////    }
        ////
        ////    NativeMethods.SetWindowLongPtr(this.handle, GWL.STYLE, new IntPtr((int)dwNewStyle));
        ////    return true;
        ////}
        ////
        ////private void ForceRedrawWindowFromPropertyChanged()
        ////{
        ////    this.HandleMaximize();
        ////    if (this.handle != IntPtr.Zero)
        ////    {
        ////        UnsafeNativeMethods.RedrawWindow(this.handle, IntPtr.Zero, IntPtr.Zero, Constants.RedrawWindowFlags.Invalidate | Constants.RedrawWindowFlags.Frame);
        ////    }
        ////}

        private bool isCleanedUp;

        private void Cleanup()
        {
            if (!this.isCleanedUp)
            {
                this.isCleanedUp = true;

                // clean up events
                ////if (this.AssociatedObject is MetroWindow)
                ////{
                ////    System.ComponentModel.DependencyPropertyDescriptor.FromProperty(MetroWindow.IgnoreTaskbarOnMaximizeProperty, typeof(MetroWindow))
                ////          .RemoveValueChanged(this.AssociatedObject, this.IgnoreTaskbarOnMaximizePropertyChangedCallback);
                ////    System.ComponentModel.DependencyPropertyDescriptor.FromProperty(MetroWindow.UseNoneWindowStyleProperty, typeof(MetroWindow))
                ////          .RemoveValueChanged(this.AssociatedObject, this.UseNoneWindowStylePropertyChangedCallback);
                ////}
                this.AssociatedObject.Loaded -= this.AssociatedObject_Loaded;
                this.AssociatedObject.Unloaded -= this.AssociatedObject_Unloaded;
                this.AssociatedObject.SourceInitialized -= this.AssociatedObject_SourceInitialized;
                this.AssociatedObject.StateChanged -= this.OnAssociatedObjectHandleMaximize;
                if (this.hwndSource != null)
                {
                    this.hwndSource.RemoveHook(this.WindowProc);
                }
                this.windowChrome = null;
            }
        }

        /// <summary>
        /// Unhooks any necessary handlers from the AssociatedObject.
        /// </summary>
        protected override void OnDetaching()
        {
            this.Cleanup();
            base.OnDetaching();
        }

        private void AssociatedObject_Unloaded(object sender, RoutedEventArgs e)
        {
            this.Cleanup();
        }

        private IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            var returnval = IntPtr.Zero;

            switch (msg)
            {
                case Constants.WM_NCPAINT:
                    handled = true;
                    break;
                case Constants.WM_NCACTIVATE:
                    /* As per http://msdn.microsoft.com/en-us/library/ms632633(VS.85).aspx , "-1" lParam "does not repaint the nonclient area to reflect the state change." */
                    returnval = UnsafeNativeMethods.DefWindowProc(hwnd, msg, wParam, new IntPtr(-1));
                    handled = true;
                    break;
            }

            return returnval;
        }

        private void OnAssociatedObjectHandleMaximize(object sender, EventArgs e)
        {
            this.HandleMaximize();
        }

        private void HandleMaximize()
        {
            this.borderThicknessChangeNotifier.ValueChanged -= this.BorderThicknessChangeNotifierOnValueChanged;
            this.topMostChangeNotifier.ValueChanged -= this.TopMostChangeNotifierOnValueChanged;

            var metroWindow = this.AssociatedObject as MetroWindow;
            ////var enableDWMDropShadow = this.EnableDWMDropShadow;
            ////if (metroWindow != null)
            ////{
            ////    enableDWMDropShadow = metroWindow.GlowBrush == null && (metroWindow.EnableDWMDropShadow || this.EnableDWMDropShadow);
            ////}

            if (this.AssociatedObject.WindowState == WindowState.Maximized)
            {
                // remove window border, so we can move the window from top monitor position
                this.AssociatedObject.BorderThickness = new Thickness(0);

                ////var ignoreTaskBar = metroWindow != null && metroWindow.IgnoreTaskbarOnMaximize;
                if (this.handle != IntPtr.Zero)
                {
                    this.windowChrome.ResizeBorderThickness = new Thickness(0);

                    // WindowChrome handles the size false if the main monitor is lesser the monitor where the window is maximized
                    // so set the window pos/size twice
                    IntPtr monitor = UnsafeNativeMethods.MonitorFromWindow(this.handle, Constants.MONITOR_DEFAULTTONEAREST);
                    if (monitor != IntPtr.Zero)
                    {
                        var monitorInfo = new MONITORINFO();
                        UnsafeNativeMethods.GetMonitorInfo(monitor, monitorInfo);

                        ////if (ignoreTaskBar)
                        ////{
                        ////    var x = monitorInfo.rcMonitor.left;
                        ////    var y = monitorInfo.rcMonitor.top;
                        ////    var cx = Math.Abs(monitorInfo.rcMonitor.right - x);
                        ////    var cy = Math.Abs(monitorInfo.rcMonitor.bottom - y);
                        ////
                        ////    var trayHWND = Standard.NativeMethods.FindWindow("Shell_TrayWnd", null);
                        ////    if (this.isWindwos10OrHigher && trayHWND != IntPtr.Zero)
                        ////    {
                        ////        UnsafeNativeMethods.SetWindowPos(this.handle, trayHWND, x, y, cx, cy, 0x0040);
                        ////        Standard.NativeMethods.ShowWindow(this.handle, Standard.SW.HIDE);
                        ////        Standard.NativeMethods.ShowWindow(this.handle, Standard.SW.SHOW);
                        ////    }
                        ////    else
                        ////    {
                        ////        UnsafeNativeMethods.SetWindowPos(this.handle, new IntPtr(-2), x, y, cx, cy, 0x0040);
                        ////    }
                        ////}
                        ////else
                        {
                            var x = monitorInfo.rcWork.left;
                            var y = monitorInfo.rcWork.top;
                            var cx = Math.Abs(monitorInfo.rcWork.right - x);
                            var cy = Math.Abs(monitorInfo.rcWork.bottom - y);

                            UnsafeNativeMethods.SetWindowPos(this.handle, new IntPtr(-2), x, y, cx, cy, 0x0040);
                        }
                    }
                }
            }
            else
            {
                ////if (!enableDWMDropShadow)
                ////{
                ////    this.AssociatedObject.BorderThickness = this.savedBorderThickness.GetValueOrDefault(new Thickness(0));
                ////}
                var resizeBorderThickness = this.savedResizeBorderThickness.GetValueOrDefault(new Thickness(0));
                if (this.windowChrome.ResizeBorderThickness != resizeBorderThickness)
                {
                    this.windowChrome.ResizeBorderThickness = resizeBorderThickness;
                }
            }

            // fix nasty TopMost bug
            // - set TopMost="True"
            // - start mahapps demo
            // - TopMost works
            // - maximize window and back to normal
            // - TopMost is gone
            //
            // Problem with minimize animation when window is maximized #1528
            // 1. Activate another application (such as Google Chrome).
            // 2. Run the demo and maximize it.
            // 3. Minimize the demo by clicking on the taskbar button.
            // Note that the minimize animation in this case does actually run, but somehow the other
            // application (Google Chrome in this example) is instantly switched to being the top window,
            // and so blocking the animation view.
            this.AssociatedObject.Topmost = false;
            this.AssociatedObject.Topmost = this.AssociatedObject.WindowState == WindowState.Minimized || this.savedTopMost;

            this.borderThicknessChangeNotifier.ValueChanged += this.BorderThicknessChangeNotifierOnValueChanged;
            this.topMostChangeNotifier.ValueChanged += this.TopMostChangeNotifierOnValueChanged;
        }

        private void AssociatedObject_SourceInitialized(object sender, EventArgs e)
        {
            this.handle = new WindowInteropHelper(this.AssociatedObject).Handle;
            if (IntPtr.Zero == this.handle)
            {
                throw new Exception("Uups, at this point we really need the Handle from the associated object!");
            }
            this.hwndSource = HwndSource.FromHwnd(this.handle);
            if (this.hwndSource != null)
            {
                this.hwndSource.AddHook(this.WindowProc);
            }

            if (this.AssociatedObject.ResizeMode != ResizeMode.NoResize)
            {
                // handle size to content (thanks @lynnx).
                // This is necessary when ResizeMode != NoResize. Without this workaround,
                // black bars appear at the right and bottom edge of the window.
                var sizeToContent = this.AssociatedObject.SizeToContent;
                var snapsToDevicePixels = this.AssociatedObject.SnapsToDevicePixels;
                this.AssociatedObject.SnapsToDevicePixels = true;
                this.AssociatedObject.SizeToContent = sizeToContent == SizeToContent.WidthAndHeight ? SizeToContent.Height : SizeToContent.Manual;
                this.AssociatedObject.SizeToContent = sizeToContent;
                this.AssociatedObject.SnapsToDevicePixels = snapsToDevicePixels;
            }

            // handle the maximized state here too (to handle the border in a correct way)
            this.HandleMaximize();
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            var window = sender as MetroWindow;
            if (window == null)
            {
                return;
            }

            if (window.ResizeMode != ResizeMode.NoResize)
            {
                //window.SetIsHitTestVisibleInChromeProperty<Border>("PART_Border");
                window.SetIsHitTestVisibleInChromeProperty<UIElement>("PART_Icon");

//// Let the resize grip handle the dragging
////                window.SetWindowChromeResizeGripDirection("PART_WindowResizeGrip", ResizeGripDirection.BottomRight);
            }
        }
    }
}
