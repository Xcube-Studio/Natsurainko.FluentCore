using PInvoke;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Navigation;
using static Natsurainko.FluentCore.Extension.Windows.Module.Authenticator.Dialog.MicrosoftAuthenticateDialog;

namespace Natsurainko.FluentCore.Extension.Windows.Module.Authenticator.Dialog;

public partial class AuthenticateDialog : Window
{
    public const string Url = "https://login.live.com/oauth20_authorize.srf?client_id=00000000402b5328" +
        "&response_type=code" +
        "&scope=XboxLive.signin%20offline_access" +
        "&redirect_uri=https://login.live.com/oauth20_desktop.srf" +
        "&prompt=";

    internal new DialogResult DialogResult { get; private set; } 
        = DialogResult.Abort;

    internal string AccessCode { get; private set; }

    internal DialogPrompt DialogPrompt { get; private set; }

    public AuthenticateDialog(DialogPrompt dialogPrompt = DialogPrompt.Login)
    {
        DialogPrompt = dialogPrompt;

        CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, CloseWindow));
        InitializeComponent();
    }

    private void SetRect()
    {
        var presentationSource = PresentationSource.FromVisual(Grid);

        var (dpiX, dpiY) =
            (presentationSource.CompositionTarget.TransformToDevice.M11,
            presentationSource.CompositionTarget.TransformToDevice.M22);

        IntPtr handle = WebBrowser.Handle;
        DllImports.SetWindowRgn(handle, IntPtr.Zero, true);

        Rect PanelRect = new(new Size(
            Grid.ActualWidth * dpiX,
            Grid.ActualHeight * dpiY));

        var C1 = DllImports.CreateRectRgn(0, 0, (int)PanelRect.BottomRight.X, (int)PanelRect.BottomRight.Y);

        var UIRect = new Rect(new Size(CloseGrid.ActualWidth * dpiX, CloseGrid.ActualHeight * dpiY));

        var D1 = (int)(CloseGrid.TransformToAncestor(Grid).Transform(new Point(0, 0)).X * dpiX);
        var D2 = (int)(CloseGrid.TransformToAncestor(Grid).Transform(new Point(0, 0)).Y * dpiY);
        var D3 = D1 + (int)UIRect.Width;
        var D4 = D2 + (int)UIRect.Height;

        var C2 = DllImports.CreateRectRgn(D1, D2, D3, D4);

        DllImports.CombineRgn(C1, C1, C2, 4);
        DllImports.SetWindowRgn(handle, C1, true);
    }

    private void CloseWindow(object _, ExecutedRoutedEventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        SystemCommands.CloseWindow(this);
    }

    private void Window_Loaded(object _, RoutedEventArgs e)
    {
        WebBrowser.Navigate(Url + DialogPrompt.ToString().ToLower());
        var hwnd = new WindowInteropHelper(this).Handle;
        User32.SetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_STYLE, (User32.SetWindowLongFlags)(User32.GetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_STYLE) & ~0x00080000));

        SetRect();
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize != new Size(485, 530))
        {
            this.Width = 485;
            this.Height = 530;
        }
    }

    private void WebBrowser_Navigated(object _, NavigationEventArgs e) => WebBrowser.Visibility = Visibility.Visible;

    private void WebBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
    {
        if (e.Uri.ToString().Contains("error=access_denied"))
        {
            DialogResult = DialogResult.Cancel;
            this.Close();
        }
        else if (e.Uri.ToString().Contains("error="))
            this.Close();
        else if (e.Uri.ToString().Contains("code="))
        {
            DialogResult = DialogResult.OK;
            AccessCode = e.Uri.Query.Replace("?code=", string.Empty).Split('&')[0];
            this.Close();
        }
    }
}
