using System;
using System.Security.Cryptography;
using System.Text;

namespace Nrk.FluentCore.Authentication.Offline;

public class DefaultOfflineAuthenticator : IAuthenticator<OfflineAccount>
{
    private readonly string _name;
    private readonly Guid _uuid;

    /// <summary>
    /// Creates a new <see cref="DefaultOfflineAuthenticator"/> with the given name and UUID. UUID will be generated from <paramref name="name"/> if not provided.
    /// </summary>
    /// <param name="name">Name of the account</param>
    /// <param name="uuid">UUID of the account (will be generated form <paramref name="name"/> if <see cref="null"/> is provided)</param>
    /// <exception cref="ArgumentNullException">Throws if <paramref name="name"/> is null</exception>
    public DefaultOfflineAuthenticator(string name, Guid? uuid = null)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _uuid = uuid ?? new Guid(MD5.HashData(Encoding.UTF8.GetBytes(name)));
    }

    #region IAuthenticator<OfflineAccount> Members

    public OfflineAccount Authenticate() => new
    (
        Name: _name,
        Uuid: _uuid,
        AccessToken: Guid.NewGuid().ToString("N")
    );

    OfflineAccount[] IAuthenticator<OfflineAccount>.Authenticate() => new[] { Authenticate() };

    #endregion
}
