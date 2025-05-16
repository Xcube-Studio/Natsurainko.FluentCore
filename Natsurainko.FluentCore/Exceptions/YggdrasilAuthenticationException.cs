using System;
using System.Text.Json.Nodes;

namespace Nrk.FluentCore.Exceptions;

public class YggdrasilAuthenticationException(
    string responseJson, 
    Exception? innerException = null) : Exception(ParseUnicodeContent(responseJson), innerException)
{
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