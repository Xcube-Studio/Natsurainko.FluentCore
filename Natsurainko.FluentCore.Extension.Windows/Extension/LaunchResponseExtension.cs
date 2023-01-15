using Natsurainko.FluentCore.Model.Launch;
using PInvoke;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace Natsurainko.FluentCore.Extension.Windows.Extension;

[SupportedOSPlatform("windows")]
public static class LaunchResponseExtension
{
    public static void SetMainWindowTitle(this LaunchResponse launchResponse, string title, int frequency = 500) => Task.Run(() =>
    {
        launchResponse.Process?.WaitForInputIdle();

        string raw = launchResponse.Process.MainWindowTitle;

        Task.Run(async () =>
        {
            try
            {
                while (!(launchResponse.Process?.HasExited).GetValueOrDefault(true))
                {
                    if (launchResponse.Process != null && launchResponse.Process?.MainWindowTitle != title)
                        User32.SetWindowText(launchResponse.Process.MainWindowHandle, title);

                    await Task.Delay(frequency);
                    launchResponse.Process?.Refresh();
                }
            }
            catch //(Exception ex)
            {
                //throw;
            }
        });
    });
}
