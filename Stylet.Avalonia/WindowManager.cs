﻿using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Media;
using Stylet.Avalonia;
using Stylet.Avalonia.Primitive;
using Stylet.Logging;

namespace Stylet;

/// <summary>
///     Manager capable of taking a ViewModel instance, instantiating its View and showing it as a dialog or window
/// </summary>
public interface IWindowManager
{
    /// <summary>
    ///     Given a ViewModel, show its corresponding View as a window
    /// </summary>
    /// <param name="viewModel">ViewModel to show the View for</param>
    void ShowWindow(object viewModel);

    /// <summary>
    ///     Given a ViewModel, show its corresponding View as a window, and set its owner
    /// </summary>
    /// <param name="viewModel">ViewModel to show the View for</param>
    /// <param name="ownerViewModel">The ViewModel for the View which should own this window</param>
    void ShowWindow(object viewModel, IViewAware ownerViewModel);

    /// <summary>
    ///     Given a ViewModel, show its corresponding View as a Dialog
    /// </summary>
    /// <param name="viewModel">ViewModel to show the View for</param>
    /// <returns>DialogResult of the View</returns>
    Task<T> ShowDialog<T>(object viewModel);

    /// <summary>
    ///     Given a ViewModel, show its corresponding View as a Dialog, and set its owner
    /// </summary>
    /// <param name="viewModel">ViewModel to show the View for</param>
    /// <param name="ownerViewModel">The ViewModel for the View which should own this dialog</param>
    /// <returns>DialogResult of the View</returns>
    Task<T> ShowDialog<T>(object viewModel, IViewAware ownerViewModel);

    /// <summary>
    ///     Display a MessageBox
    /// </summary>
    /// <param name="text">A <see cref="System.String" /> that specifies the text to display.</param>
    /// <param name="caption">A <see cref="System.String" /> that specifies the title bar caption to display.</param>
    /// <param name="buttons">A <see cref="MessageBoxButton" /> value that specifies which button or buttons to display.</param>
    /// <param name="icon">A <see cref="MessageBoxImage" /> value that specifies the icon to display.</param>
    /// <param name="defaultResult">
    ///     A <see cref="MessageBoxResult" /> value that specifies the default result of the message
    ///     box.
    /// </param>
    /// <param name="cancelResult">A <see cref="MessageBoxResult" /> value that specifies the cancel result of the message box</param>
    /// <param name="flowDirection">
    ///     The <see cref="FlowDirection" /> to use, overrides the
    ///     <see>
    ///         <cref>MessageBoxViewModel.DefaultFlowDirection</cref>
    ///     </see>
    /// </param>
    /// <param name="textAlignment">
    ///     The <see cref="TextAlignment" /> to use, overrides the
    ///     <see>
    ///         <cref>MessageBoxViewModel.DefaultTextAlignment</cref>
    ///     </see>
    /// </param>
    /// <returns>The result chosen by the user</returns>
    Task<T> ShowMessageBox<T>(
        string text,
        string? caption = null,
        MessageBoxButton buttons = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.None,
        MessageBoxResult defaultResult = MessageBoxResult.OK,
        MessageBoxResult cancelResult = MessageBoxResult.None,
        FlowDirection flowDirection = FlowDirection.LeftToRight,
        TextAlignment textAlignment = TextAlignment.Left);
}

/// <summary>
///     Configuration passed to WindowManager (normally implemented by StyletApplicationBase)
/// </summary>
public interface IWindowManagerConfig
{
    /// <summary>
    ///     Returns the currently-displayed window, or null if there is none (or it can't be determined)
    /// </summary>
    /// <returns>The currently-displayed window, or null</returns>
    AvaloniaObject? GetActiveWindow();
}

/// <summary>
///     Default implementation of IWindowManager, is capable of showing a ViewModel's View as a dialog or a window
/// </summary>
public class WindowManager : IWindowManager
{
    private static readonly ILogger _logger = LogManager.GetLogger(typeof(WindowManager));
    private readonly Func<AvaloniaObject?> _getActiveWindow;
    private readonly IViewManager _viewManager;

    /// <summary>
    ///     Initialises a new instance of the <see cref="WindowManager" /> class, using the given <see cref="IViewManager" />
    /// </summary>
    /// <param name="viewManager">IViewManager to use when creating views</param>
    /// <param name="config">Configuration object</param>
    public WindowManager(IViewManager viewManager, IWindowManagerConfig config)
    {
        _viewManager = viewManager;
        _getActiveWindow = config.GetActiveWindow;
    }

    /// <summary>
    ///     Given a ViewModel, show its corresponding View as a window
    /// </summary>
    /// <param name="viewModel">ViewModel to show the View for</param>
    public void ShowWindow(object viewModel)
    {
        ShowWindow(viewModel, null);
    }

    /// <summary>
    ///     Given a ViewModel, show its corresponding View as a window, and set its owner
    /// </summary>
    /// <param name="viewModel">ViewModel to show the View for</param>
    /// <param name="ownerViewModel">The ViewModel for the View which should own this window</param>
    public void ShowWindow(object viewModel, IViewAware? ownerViewModel)
    {
        CreateWindow(viewModel, false, ownerViewModel).Show();
    }

    /// <summary>
    ///     Given a ViewModel, show its corresponding View as a Dialog
    /// </summary>
    /// <param name="viewModel">ViewModel to show the View for</param>
    /// <returns>DialogResult of the View</returns>
    public Task<T> ShowDialog<T>(object viewModel)
    {
        return ShowDialog<T>(viewModel, null);
    }

    /// <summary>
    ///     Given a ViewModel, show its corresponding View as a Dialog, and set its owner
    /// </summary>
    /// <param name="viewModel">ViewModel to show the View for</param>
    /// <param name="ownerViewModel">The ViewModel for the View which should own this dialog</param>
    /// <returns>DialogResult of the View</returns>
    public Task<T> ShowDialog<T>(object viewModel, IViewAware? ownerViewModel)
    {
        var window = CreateWindow(viewModel, true, ownerViewModel);
        return window.ShowDialog<T>((Window)window.Owner!);
    }

    /// <summary>
    ///     Display a MessageBox
    /// </summary>
    /// <param name="text">A <see cref="System.String" /> that specifies the text to display.</param>
    /// <param name="caption">A <see cref="System.String" /> that specifies the title bar caption to display.</param>
    /// <param name="buttons">A <see cref="MessageBoxButton" /> value that specifies which button or buttons to display.</param>
    /// <param name="icon">A <see cref="MessageBoxImage" /> value that specifies the icon to display.</param>
    /// <param name="defaultResult">
    ///     A <see cref="MessageBoxResult" /> value that specifies the default result of the message
    ///     box.
    /// </param>
    /// <param name="cancelResult">A <see cref="MessageBoxResult" /> value that specifies the cancel result of the message box</param>
    /// <param name="flowDirection">
    ///     The <see cref="FlowDirection" /> to use, overrides the
    ///     <see>
    ///         <cref>MessageBoxViewModel.DefaultFlowDirection</cref>
    ///     </see>
    /// </param>
    /// <param name="textAlignment">
    ///     The <see cref="TextAlignment" /> to use, overrides the
    ///     <see>
    ///         <cref>MessageBoxViewModel.DefaultTextAlignment</cref>
    ///     </see>
    /// </param>
    /// <returns>The result chosen by the user</returns>
    public Task<T> ShowMessageBox<T>(
        string text,
        string? caption = null,
        MessageBoxButton buttons = MessageBoxButton.OK,
        MessageBoxImage icon = MessageBoxImage.None,
        MessageBoxResult defaultResult = MessageBoxResult.OK,
        MessageBoxResult cancelResult = MessageBoxResult.None,
        FlowDirection flowDirection = FlowDirection.LeftToRight,
        TextAlignment textAlignment = TextAlignment.Left)
    {
        var vm = IoC.Get<IMessageBoxViewModel>();
        vm.Setup(text, caption, buttons, icon, defaultResult, cancelResult, flowDirection, textAlignment);
        return ShowDialog<T>(vm);
    }

    /// <summary>
    ///     Given a ViewModel, create its View, ensure that it's a Window, and set it up
    /// </summary>
    /// <param name="viewModel">ViewModel to create the window for</param>
    /// <param name="isDialog">True if the window will be used as a dialog</param>
    /// <param name="ownerViewModel">Optionally the ViewModel which owns the view which should own this window</param>
    /// <returns>Window which was created and set up</returns>
    protected virtual Window CreateWindow(object viewModel, bool isDialog, IViewAware? ownerViewModel)
    {
        var view = _viewManager.CreateAndBindViewForModelIfNecessary(viewModel);
        if (view is not Window window)
        {
            var e = new StyletInvalidViewTypeException(
                $"WindowManager.ShowWindow or .ShowDialog tried to show a View of type '{view.GetType().Name}', but that View doesn't derive from the Window class. " +
                "Make sure any Views you display using WindowManager.ShowWindow or .ShowDialog derive from Window (not UserControl, etc)");
            _logger.Error(e);
            throw e;
        }

        // Only set this it hasn't been set / bound to anything
        if (viewModel is IHaveDisplayName haveDisplayName &&
            (string.IsNullOrEmpty(window.Title) ||
             window.Title ==
             view.GetType().Name) /*&& BindingOperations.GetBindingBase(window, Window.TitleProperty) == null*/)
        {
            var binding = new Binding(nameof(IHaveDisplayName.DisplayName))
            {
                Source = haveDisplayName.DisplayName,
                Mode = BindingMode.TwoWay
            };
            window.Bind(Window.TitleProperty, binding);
        }

        if (ownerViewModel?.View is Window explicitOwner)
        {
            window.SetValue(WindowBase.OwnerProperty, explicitOwner);
        }
        else if (isDialog)
        {
            var owner = InferOwnerOf(window);
            if (owner is not null)
                // We can end up in a really weird situation if they try and display more than one dialog as the application's closing
                // Basically the MainWindow's no long active, so the second dialog chooses the first dialog as its owner... But the first dialog
                // hasn't yet been shown, so we get an exception ("cannot set owner property to a Window which has not been previously shown").
                try
                {
                    // window.Owner = owner;
                    // window.SetValue(Window.OwnerProperty, owner);
                    var propertyInfo = typeof(WindowBase).GetProperty(nameof(Window.Owner),
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (propertyInfo is not null && propertyInfo.CanWrite) propertyInfo.SetValue(window, owner); // 设置新值
                }
                catch (InvalidOperationException e)
                {
                    _logger.Error(e, "This can occur when the application is closing down");
                }
        }

        if (isDialog)
            _logger.Info("Displaying ViewModel {0} with View {1} as a Dialog", viewModel, window);
        else
            _logger.Info("Displaying ViewModel {0} with View {1} as a Window", viewModel, window);

        // If and only if they haven't tried to position the window themselves...
        // Has to be done after we're attempted to set the owner
        if (window.WindowStartupLocation == WindowStartupLocation.Manual && double.IsNaN(window.Position.Y) &&
            double.IsNaN(window.Position.X)
            /*&& BindingOperations.GetBinding(window, Window.TopProperty) == null && BindingOperations.GetBinding(window, Window.LeftProperty) == null*/
           )
            // var topObservable = window.GetBindingSubject(Control.TagProperty);
            // var leftObservable = window.GetBindingSubject(Control.LeftProperty);
            window.WindowStartupLocation = window.Owner == null
                ? WindowStartupLocation.CenterScreen
                : WindowStartupLocation.CenterOwner;

        // This gets itself retained by the window, by registering events
        // ReSharper disable once ObjectCreationAsStatement
        new WindowConductor(window, viewModel);

        return window;
    }

    private Window? InferOwnerOf(Window window)
    {
        var active = _getActiveWindow() as Window;
        return ReferenceEquals(active, window) ? null : active;
    }

    private class WindowConductor : IChildDelegate
    {
        private readonly object _viewModel;
        private readonly Window _window;
        private readonly IDisposable? _windowStateChangedObservable;

        public WindowConductor(Window window, object viewModel)
        {
            _window = window;
            _viewModel = viewModel;

            // They won't be able to request a close unless they implement IChild anyway...
            var viewModelAsChild = _viewModel as IChild;
            if (viewModelAsChild != null)
                viewModelAsChild.Parent = this;

            ScreenExtensions.TryActivate(_viewModel);

            var viewModelAsScreenState = _viewModel as IScreenState;
            _windowStateChangedObservable = null;
            if (viewModelAsScreenState != null)
            {
                // window.StateChanged += this.WindowStateChanged;
                _windowStateChangedObservable = window.GetPropertyChangedObservable(Window.WindowStateProperty)
                    .Subscribe(WindowStateChanged);
                window.Closed += WindowClosed;
            }

            if (_viewModel is IGuardClose)
                window.Closing += WindowClosing;
        }

        /// <summary>
        ///     Close was requested by the child
        /// </summary>
        /// <param name="item">Item to close</param>
        /// <param name="dialogResult">DialogResult to close with, if it's a dialog</param>
        async void IChildDelegate.CloseItem(object item, bool? dialogResult)
        {
            if (item != _viewModel)
            {
                _logger.Warn("IChildDelegate.CloseItem called with item {0} which is _not_ our ViewModel {1}", item,
                    _viewModel);
                return;
            }

            if (_viewModel is IGuardClose guardClose && !await guardClose.CanCloseAsync())
            {
                _logger.Info("Close of ViewModel {0} cancelled because CanCloseAsync returned false", _viewModel);
                return;
            }

            _logger.Info("ViewModel {0} close requested with DialogResult {1} because it called RequestClose",
                _viewModel,
                dialogResult ?? false);

            // this.window.StateChanged -= this.WindowStateChanged;
            _windowStateChangedObservable?.Dispose();
            _window.Closed -= WindowClosed;
            _window.Closing -= WindowClosing;

            // Need to call this after unregistering the event handlers, as it causes the window
            // to be closed
            // TODO:
            // if (dialogResult != null)
            //     this.window.DialogResult = dialogResult;

            ScreenExtensions.TryClose(_viewModel);

            _window.Close(dialogResult);
        }

        private void WindowStateChanged(AvaloniaPropertyChangedEventArgs e)
        {
            switch (_window.WindowState)
            {
                case WindowState.Maximized:
                case WindowState.Normal:
                    _logger.Info("Window {0} maximized/restored: activating", _window);
                    ScreenExtensions.TryActivate(_viewModel);
                    break;

                case WindowState.Minimized:
                    _logger.Info("Window {0} minimized: deactivating", _window);
                    ScreenExtensions.TryDeactivate(_viewModel);
                    break;
            }
        }

        private void WindowClosed(object? sender, EventArgs e)
        {
            // Logging was done in the Closing handler

            // this.window.StateChanged -= this.WindowStateChanged;
            _windowStateChangedObservable?.Dispose();
            _window.Closed -= WindowClosed;
            _window.Closing -= WindowClosing; // Not sure this is required

            ScreenExtensions.TryClose(_viewModel);
        }

        private async void WindowClosing(object? sender, CancelEventArgs e)
        {
            if (e.Cancel)
                return;

            _logger.Info("ViewModel {0} close requested because its View was closed", _viewModel);

            // See if the task completed synchronously
            var task = ((IGuardClose)_viewModel).CanCloseAsync();
            if (task.IsCompleted)
            {
                // The closed event handler will take things from here if we don't cancel
                if (!task.Result)
                    _logger.Info("Close of ViewModel {0} cancelled because CanCloseAsync returned false", _viewModel);
                e.Cancel = !task.Result;
            }
            else
            {
                e.Cancel = true;
                _logger.Info("Delaying closing of ViewModel {0} because CanCloseAsync is completing asynchronously",
                    _viewModel);
                if (await task)
                {
                    _window.Closing -= WindowClosing;
                    _window.Close();
                    // The Closed event handler handles unregistering the events, and closing the ViewModel
                }
                else
                {
                    _logger.Info("Close of ViewModel {0} cancelled because CanCloseAsync returned false", _viewModel);
                }
            }
        }
    }
}