using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nrk.FluentCore.Experimental.GameManagement.Downloader;
using Nrk.FluentCore.Experimental.GameManagement.Installer;
using Nrk.FluentCore.Experimental.GameManagement.Installer.Data;
using Nrk.FluentCore.Utils;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Threading;

namespace InstanceInstallerWPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        this.DataContext = new ViewModel();
    }
}

#nullable disable
partial class ViewModel : ObservableObject
{
    private CancellationTokenSource cancellationTokenSource;

    private readonly Dispatcher Dispatcher = App.Current.Dispatcher;

    public ViewModel()
    {
        Task.Run(async () =>
        {
            var httpClient = HttpUtils.HttpClient;

            string requestUrl = "http://launchermeta.mojang.com/mc/game/version_manifest_v2.json";

            using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            using var responseMessage = await httpClient.SendAsync(requestMessage);
            responseMessage.EnsureSuccessStatusCode();

            string jsonContent = await responseMessage.Content.ReadAsStringAsync();
            VersionManifestJsonObject versionManifest = JsonNode.Parse(jsonContent).Deserialize<VersionManifestJsonObject>()!;

            Dispatcher.Invoke(() =>
            {
                ManifestItems = versionManifest.Versions;
                SelectedItem = ManifestItems.FirstOrDefault();
            });
        });
    }

    [ObservableProperty]
    private VersionManifestItem[] manifestItems;

    [ObservableProperty]
    private VersionManifestItem selectedItem;

    [ObservableProperty]
    private string minecraftFolder = @"D:\Minecraft\Test\.minecraft";

    [ObservableProperty]
    private string text;

    [ObservableProperty]
    private IEnumerable<IProgressReporter.ProgressData> progressDatas;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool canCancel = false;

    [RelayCommand]
    Task Install() => Task.Run(async () =>
    {
        cancellationTokenSource = new CancellationTokenSource();
        Dispatcher.Invoke(() => CanCancel = true);

        var vanillaInstanceInstaller = new VanillaInstanceInstaller()
        {
            DownloadMirror = DownloadMirrors.BmclApi,
            McVersionManifestItem = SelectedItem,
            MinecraftFolder = MinecraftFolder,
            CheckAllDependencies = true
        };

        Dispatcher.Invoke(() => ProgressDatas = vanillaInstanceInstaller.Progresses.Values);

        vanillaInstanceInstaller.ProgressChanged += (object sender, IProgressReporter.ProgressUpdater e)
            => Dispatcher.BeginInvoke(() => e.Update(vanillaInstanceInstaller.Progresses));

        try
        {
            var instance = await vanillaInstanceInstaller.InstallAsync(cancellationTokenSource.Token);
            Dispatcher.Invoke(() => Text = $"Minecraft {instance.InstanceId} successfully installed");
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => Text = ex.ToString());
        }
    });

    [RelayCommand(CanExecute = nameof(CanCancel))]
    void Cancel() 
    {
        cancellationTokenSource.Cancel();
        CanCancel = false;
    }
}