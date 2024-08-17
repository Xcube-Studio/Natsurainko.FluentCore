using System;
using System.Text.Json.Nodes;

namespace Nrk.FluentCore.Authentication;

public class YggdrasilAuthenticationException : Exception
{
    public YggdrasilAuthenticationException(string responseJson)
        : base(ParseUnicodeContent(responseJson)) { }

    private static string ParseUnicodeContent(string content)
    {
        try
        {
            var jsonNode = JsonNode.Parse(content);
            var errorMessage = jsonNode!["errorMessage"]!.GetValue<string>();

            return $"Yggdrasil authentication failed with response\r\n{errorMessage}";
        }
        catch (Exception) { }

        return $"Yggdrasil authentication failed with response\r\n{content}";
    }
}
