using Nrk.FluentCore.Authentication;
using System.Collections.ObjectModel;

namespace Nrk.FluentCore.Services.Accounts;

public interface IAccountService
{
    public Account? ActiveAccount { get; }

    public ReadOnlyObservableCollection<Account> Accounts { get; }

    void ActivateAccount(Account? account);

    void AddAccount(Account account);

    bool RemoveAccount(Account account);
}
