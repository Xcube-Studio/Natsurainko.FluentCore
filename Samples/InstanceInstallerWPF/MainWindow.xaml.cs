using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nrk.FluentCore.Environment;
using Nrk.FluentCore.GameManagement.Downloader;
using Nrk.FluentCore.GameManagement.Installer;
using Nrk.FluentCore.Utils;
using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Threading;
using static Nrk.FluentCore.GameManagement.Installer.FabricInstanceInstaller;
using static Nrk.FluentCore.GameManagement.Installer.ForgeInstanceInstaller;
using static Nrk.FluentCore.GameManagement.Installer.OptiFineInstanceInstaller;
using static Nrk.FluentCore.GameManagement.Installer.QuiltInstanceInstaller;
using static Nrk.FluentCore.GameManagement.Installer.VanillaInstanceInstaller;

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
    private readonly string JavaPath = JavaUtils.SearchJava().Select(JavaUtils.GetJavaInfo).MaxBy(x => x.Version).FilePath;

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
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private VersionManifestItem selectedItem;

    [ObservableProperty]
    private object[] installDatas;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private object selectedInstallData;

    [ObservableProperty]
    private string[] loaders = ["Forge", "NeoForge", "OptiFine", "Fabric", "Quilt"];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    private string selectedLoader = "Forge";

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

        if (e.PropertyName == nameof(SelectedItem) || e.PropertyName == nameof(SelectedLoader))
            Task.Run(RefreshInstallData);
    }

    bool CanInstall() => SelectedItem != null
        && SelectedLoader != null
        && SelectedInstallData != null;

    async void RefreshInstallData()
    {
        Dispatcher.Invoke(() =>
        {
            InstallDatas = null;
            SelectedInstallData = null;
        });

        string domain = SelectedLoader switch
        {
            "Forge" => "https://bmclapi2.bangbang93.com/forge/minecraft/",
            "NeoForge" => "https://bmclapi2.bangbang93.com/neoforge/list/",
            "OptiFine" => "https://bmclapi2.bangbang93.com/optifine/",
            "Fabric" => "https://meta.fabricmc.net/v2/versions/loader/",
            "Quilt" => "https://meta.quiltmc.org/v3/versions/loader/",
            _ => throw new InvalidOperationException()
        };

        string requestUrl = $"{domain}{SelectedItem.Id}";

        using var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        using var responseMessage = await httpClient.SendAsync(requestMessage);
        responseMessage.EnsureSuccessStatusCode();

        string jsonContent = await responseMessage.Content.ReadAsStringAsync();
        object[] installDatas = SelectedLoader switch
        {
            "Forge" => JsonNode.Parse(jsonContent).Deserialize<ForgeInstallData[]>()!,
            "NeoForge" => JsonNode.Parse(jsonContent).Deserialize<ForgeInstallData[]>()!,
            "OptiFine" => JsonNode.Parse(jsonContent).Deserialize<OptiFineInstallData[]>()!,
            "Fabric" => JsonNode.Parse(jsonContent).Deserialize<FabricInstallData[]>()!,
            "Quilt" => JsonNode.Parse(jsonContent).Deserialize<QuiltInstallData[]>()!,
            _ => throw new InvalidOperationException()
        };

        Dispatcher.Invoke(() =>
        {
            InstallDatas = installDatas;
            SelectedInstallData = InstallDatas.FirstOrDefault();
        });

    }
    IInstanceInstaller GetInstanceInstaller(out IReadOnlyList<InstallationStageViewModel> installationStageViews)
    {
        installationStageViews = GetInstallationViewModel(out var vanillaStagesViewModel, out var stagesViewModel);

        IInstanceInstaller installer = SelectedLoader switch
        {
            "Forge" => new ForgeInstanceInstaller()
            {
                //DownloadMirror = DownloadMirrors.BmclApi,
                McVersionManifestItem = SelectedItem,
                MinecraftFolder = MinecraftFolder,
                CheckAllDependencies = true,
                InstallData = (ForgeInstallData)SelectedInstallData,
                Progress = (InstallationViewModel<ForgeInstallationStage>)stagesViewModel,
                VanillaInstallationProgress = vanillaStagesViewModel,
                JavaPath = JavaPath,
                IsNeoForgeInstaller = false
            },
            "NeoForge" => new ForgeInstanceInstaller()
            {
                //DownloadMirror = DownloadMirrors.BmclApi,
                McVersionManifestItem = SelectedItem,
                MinecraftFolder = MinecraftFolder,
                CheckAllDependencies = true,
                InstallData = (ForgeInstallData)SelectedInstallData,
                Progress = (InstallationViewModel<ForgeInstallationStage>)stagesViewModel,
                VanillaInstallationProgress = vanillaStagesViewModel,
                JavaPath = JavaPath,
                IsNeoForgeInstaller = true
            },
            "OptiFine" => new OptiFineInstanceInstaller()
            {
                DownloadMirror = DownloadMirrors.BmclApi,
                McVersionManifestItem = SelectedItem,
                MinecraftFolder = MinecraftFolder,
                CheckAllDependencies = true,
                InstallData = (OptiFineInstallData)SelectedInstallData,
                Progress = (InstallationViewModel<OptiFineInstallationStage>)stagesViewModel,
                VanillaInstallationProgress = vanillaStagesViewModel,
                JavaPath = JavaPath
            },
            "Fabric" => new FabricInstanceInstaller()
            {
                DownloadMirror = DownloadMirrors.BmclApi,
                McVersionManifestItem = SelectedItem,
                MinecraftFolder = MinecraftFolder,
                CheckAllDependencies = true,
                InstallData = (FabricInstallData)SelectedInstallData,
                Progress = (InstallationViewModel<FabricInstallationStage>)stagesViewModel,
                VanillaInstallationProgress = vanillaStagesViewModel
            },
            "Quilt" => new QuiltInstanceInstaller()
            {
                DownloadMirror = DownloadMirrors.BmclApi,
                McVersionManifestItem = SelectedItem,
                MinecraftFolder = MinecraftFolder,
                CheckAllDependencies = true,
                InstallData = (QuiltInstallData)SelectedInstallData,
                Progress = (InstallationViewModel<QuiltInstallationStage>)stagesViewModel,
                VanillaInstallationProgress = vanillaStagesViewModel
            },
            _ => throw new InvalidOperationException()
        };

        return installer;
    }

    List<InstallationStageViewModel> GetInstallationViewModel(
        out InstallationViewModel<VanillaInstallationStage> vanillaStagesViewModel,
        out object stagesViewModel)
    {
        List<InstallationStageViewModel> stageViewModels = new();
        vanillaStagesViewModel = new();

        if (SelectedLoader == "Quilt")
        {
            InstallationViewModel<QuiltInstallationStage> installationViewModel = new();

            stageViewModels.AddRange(installationViewModel.Stages.Values);
            stageViewModels.InsertRange(1, vanillaStagesViewModel.Stages.Values);

            stagesViewModel = installationViewModel;
        }
        else if (SelectedLoader == "Fabric")
        {
            InstallationViewModel<FabricInstallationStage> installationViewModel = new();

            stageViewModels.AddRange(installationViewModel.Stages.Values);
            stageViewModels.InsertRange(1, vanillaStagesViewModel.Stages.Values);

            stagesViewModel = installationViewModel;
        }
        else if (SelectedLoader == "Forge" || SelectedLoader == "NeoForge")
        {
            InstallationViewModel<ForgeInstallationStage> installationViewModel = new();

            stageViewModels.AddRange(installationViewModel.Stages.Values);
            stageViewModels.InsertRange(1, vanillaStagesViewModel.Stages.Values);

            stagesViewModel = installationViewModel;
        }
        else if (SelectedLoader == "OptiFine")
        {
            InstallationViewModel<OptiFineInstallationStage> installationViewModel = new();

            stageViewModels.AddRange(installationViewModel.Stages.Values);
            stageViewModels.InsertRange(1, vanillaStagesViewModel.Stages.Values);

            stagesViewModel = installationViewModel;
        }
        else throw new InvalidOperationException();

        return stageViewModels;
    }

    [RelayCommand(CanExecute = nameof(CanInstall))]
    Task Install() => Task.Run(async () =>
    {
        cancellationTokenSource = new CancellationTokenSource();
        Dispatcher.Invoke(() =>
        {
            CanCancel = true;
            Text = string.Empty;
        });

        var instanceInstaller = GetInstanceInstaller(out var installationStageViews);
        Dispatcher.Invoke(() => ProgressDatas = installationStageViews);

        try
        {
            var instance = await instanceInstaller.InstallAsync(cancellationTokenSource.Token);
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