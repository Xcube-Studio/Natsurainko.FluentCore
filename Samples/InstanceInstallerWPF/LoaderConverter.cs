using Nrk.FluentCore.Experimental.GameManagement.Installer.Data;
using System.Globalization;
using System.Windows.Data;

namespace InstanceInstallerWPF;

internal class LoaderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ForgeInstallData forgeInstallData)
            return $"{forgeInstallData.McVersion}-{forgeInstallData.Version}{(string.IsNullOrEmpty(forgeInstallData.Branch) ? string.Empty : $"-{forgeInstallData.Branch}")}";
        else if (value is OptiFineInstallData optiFineInstallData)
            return $"{optiFineInstallData.Type}_{optiFineInstallData.Patch}";
        else if (value is FabricInstallData fabricInstallData)
            return $"{fabricInstallData.Intermediary.Version}-{fabricInstallData.Loader.Version}";
        else if (value is QuiltInstallData quiltInstallData)
            return $"{quiltInstallData.Intermediary.Version}-{quiltInstallData.Loader.Version}";

        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
