using System;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace Natsurainko.FluentCore.Extension.Windows.Module.Authenticator.Dialog;

[SupportedOSPlatform("windows7.0")]
public class MicrosoftAuthenticateDialog
{
    public enum DialogPrompt
    {
        Login = 0,
        None = 1,
        Consent = 2,
        Select_Account = 3
    }

    public enum DialogResult
    {
        Abort = 3,
        Cancel = 2,
        OK = 1
    }

    public string AccessCode { get; private set; }

    public DialogResult Result { get; private set; }

    public DialogPrompt Prompt { get; set; } = DialogPrompt.Login;

    public IntPtr ParentWindowHandle { get; set; }

    public MicrosoftAuthenticateDialog()
    {

    }

    public DialogResult ShowDialog()
    {
        var thread = new Thread(() =>
        {
            var dialog = new AuthenticateDialog(Prompt);

            if (ParentWindowHandle != IntPtr.Zero)
                new WindowInteropHelper(dialog).Owner = ParentWindowHandle;

            dialog.ShowDialog();
            Result = dialog.DialogResult;

            if (Result == DialogResult.OK)
                AccessCode = dialog.AccessCode;
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        return Result;
    }

    public Task<DialogResult> ShowDialogAsync() => Task.Run(ShowDialog);
}