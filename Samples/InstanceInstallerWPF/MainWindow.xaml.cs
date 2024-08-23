using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nrk.FluentCore.Experimental.GameManagement.Downloader;
using Nrk.FluentCore.Experimental.GameManagement.Installer;
using Nrk.FluentCore.Experimental.GameManagement.Installer.Data;
using Nrk.FluentCore.Utils;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Threading;
using static Nrk.FluentCore.Experimental.GameManagement.Installer.FabricInstanceInstaller;
using static Nrk.FluentCore.Experimental.GameManagement.Installer.VanillaInstanceInstaller;

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
    private readonly HttpClient httpClient = HttpUtils.HttpClient;
    private readonly Dispatcher Dispatcher = App.Current.Dispatcher;

    private CancellationTokenSource cancellationTokenSource;

    public ViewModel()
    {
        Task.Run(async () =>
        {
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
    private FabricInstallData[] fabricInstallDatas;

    [ObservableProperty]
    private FabricInstallData selectedFabricInstallData;

    [ObservableProperty]
    private string minecraftFolder = @"D:\Minecraft\Test\.minecraft";

    [ObservableProperty]
    private string text;

    [ObservableProperty]
    private IEnumerable<InstallationStageViewModel> progressDatas;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CancelCommand))]
    private bool canCancel = false;

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName == nameof(SelectedItem))
        {
            Task.Run(GetFabricInstallData);
        }
    }

    async void GetFabricInstallData()
    {
        string requestUrl = $"https://meta.fabricmc.net/v2/versions/loader/{SelectedItem.Id}";

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        using var responseMessage = await httpClient.SendAsync(requestMessage);
        responseMessage.EnsureSuccessStatusCode();

        string jsonContent = await responseMessage.Content.ReadAsStringAsync();
        var fabricInstallDatas = JsonNode.Parse(jsonContent).Deserialize<FabricInstallData[]>()!;

        Dispatcher.Invoke(() =>
        {
            FabricInstallDatas = fabricInstallDatas;
            SelectedFabricInstallData = FabricInstallDatas.FirstOrDefault();
        });

    }

    private static readonly Dictionary<FabricInstallationStage, string> s_fabricInstallationStageNames = new()
    {
        {FabricInstallationStage.ParseOrInstallVanillaInstance, "Parse or install vanilla instance" },
        {FabricInstallationStage.DownloadFabricClientJson, "Download Fabric client.json" },
        {FabricInstallationStage.DownloadFabricLibraries, "Download Fabric libraries" },
    };

    private static readonly Dictionary<VanillaInstallationStage, string> s_vanillaInstallationStageNames = new()
    {
        {VanillaInstallationStage.DownloadVersionJson, "Download client.json" },
        {VanillaInstallationStage.DownloadAssetIndexJson, "Download asset_index.json" },
        {VanillaInstallationStage.DownloadMinecraftDependencies, "Download Minecraft dependencies" },
    };

    [RelayCommand]
    Task Install() => Task.Run(async () =>
    {
        cancellationTokenSource = new CancellationTokenSource();
        Dispatcher.Invoke(() => CanCancel = true);

        InstallationViewModel<FabricInstallationStage> stagesViewModel = new(s_fabricInstallationStageNames);
        InstallationViewModel<VanillaInstallationStage> vanillaStagesViewModel = new(s_vanillaInstallationStageNames);
        IEnumerable<InstallationStageViewModel> stages = [
            stagesViewModel.Stages[FabricInstallationStage.ParseOrInstallVanillaInstance],

            vanillaStagesViewModel.Stages[VanillaInstallationStage.DownloadVersionJson],
            vanillaStagesViewModel.Stages[VanillaInstallationStage.DownloadAssetIndexJson],
            vanillaStagesViewModel.Stages[VanillaInstallationStage.DownloadMinecraftDependencies],

            stagesViewModel.Stages[FabricInstallationStage.DownloadFabricClientJson],
            stagesViewModel.Stages[FabricInstallationStage.DownloadFabricLibraries],
        ];
        Dispatcher.Invoke(() => ProgressDatas = stages);

        var fabricInstanceInstaller = new FabricInstanceInstaller()
        {
            DownloadMirror = DownloadMirrors.BmclApi,
            McVersionManifestItem = SelectedItem,
            MinecraftFolder = MinecraftFolder,
            CheckAllDependencies = true,
            InstallData = SelectedFabricInstallData,
            Progress = stagesViewModel,
            VanillaInstallationProgress = vanillaStagesViewModel
        };

        try
        {
            var instance = await fabricInstanceInstaller.InstallAsync(cancellationTokenSource.Token);
            Dispatcher.Invoke(() => Text = $"Minecraft {instance.InstanceId} successfully installed");
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() => Text = ex.ToString());
        }

        Dispatcher.Invoke(() => CanCancel = false);
    });

    [RelayCommand(CanExecute = nameof(CanCancel))]
    void Cancel() 
    {
        cancellationTokenSource.Cancel();
        CanCancel = false;
    }
}