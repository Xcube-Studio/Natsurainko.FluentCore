using System;
using System.Security.Cryptography;
using System.Text;

namespace Nrk.FluentCore.Authentication;

public class OfflineAuthenticator
{
    /// <summary>
    /// Creates a new <see cref="DefaultOfflineAuthenticator"/> with the given name and UUID. UUID will be generated from <paramref name="name"/> if not provided.
    /// </summary>
    /// <param name="name">Name of the account</param>
    /// <param name="uuid">UUID of the account (will be generated form <paramref name="name"/> if <see cref="null"/> is provided)</param>
    /// <exception cref="ArgumentNullException">Throws if <paramref name="name"/> is null</exception>
    public OfflineAccount Login(string name, Guid? uuid = null)
        => new OfflineAccount
        (
            name ?? throw new ArgumentNullException(nameof(name)),
            uuid ?? new Guid(MD5.HashData(Encoding.UTF8.GetBytes(name))),
            Guid.NewGuid().ToString("N")
        );

    public OfflineAccount Refresh(OfflineAccount account)
        => new OfflineAccount(
            account.Name,
            account.Uuid,
            Guid.NewGuid().ToString("N")
            );
}
