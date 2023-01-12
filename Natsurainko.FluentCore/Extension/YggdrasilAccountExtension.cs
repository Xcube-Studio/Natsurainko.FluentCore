using Natsurainko.FluentCore.Model.Auth;
using Natsurainko.Toolkits.Network;
using Natsurainko.Toolkits.Text;
using System.Collections.Generic;

namespace Natsurainko.FluentCore.Extension;

public static class YggdrasilAccountExtension
{
    public static async IAsyncEnumerable<string> GetAuthlibArgumentsAsync(this YggdrasilAccount account, string authlibPath)
    {
        using var res = await HttpWrapper.HttpGetAsync(account.YggdrasilServerUrl);

        yield return $"-javaagent:{authlibPath.ToPath()}={account.YggdrasilServerUrl}";
        yield return "-Dauthlibinjector.side=client";
        yield return $"-Dauthlibinjector.yggdrasil.prefetched={(await res.Content.ReadAsStringAsync()).ConvertToBase64()}";
    }
}
