using Nrk.FluentCore.Authentication;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Nrk.FluentCore.Services.Accounts;

public class DefaultAccountService : IAccountService
{
    protected Account? _activeAccount;
    public Account? ActiveAccount
    {
        get => _activeAccount;
        protected set
        {
            if (_activeAccount != value)
                WhenActiveAccountChanged(_activeAccount, value);

            _activeAccount = value;
        }
    }

    public ReadOnlyObservableCollection<Account> Accounts { get; }
    protected readonly ObservableCollection<Account> _accounts;

    public DefaultAccountService()
    {
        _accounts = new(InitializeAccountCollection());
        Accounts = new ReadOnlyObservableCollection<Account>(_accounts);
    }

    public virtual IEnumerable<Account> InitializeAccountCollection() => Array.Empty<Account>();

    public virtual void WhenActiveAccountChanged(Account? oldAccount, Account? newAccount)
    {

    }

    public void ActivateAccount(Account? account)
    {
        if (account != null && !_accounts.Contains(account))
            throw new ArgumentException($"{account} is not an account managed by AccountService", nameof(account));

        ActiveAccount = account;
    }

    public void AddAccount(Account account)
    {
        if (Accounts.Where(x => x.Uuid.Equals(account.Uuid) && x.Type.Equals(account.Type)).Any())
            throw new Exception("不可以存在两个账户类型和 Uuid 均相同的账户");

        _accounts.Add(account);
    }

    public bool RemoveAccount(Account account)
    {
        bool result = _accounts.Remove(account);

        if (ActiveAccount == account)
            this.ActivateAccount(_accounts.Count != 0 ? _accounts[0] : null);

        return result;
    }

    public void UpdateAccount(Account account, bool isActiveAccount)
    {
        var oldAccount = Accounts.Where(x => x.Uuid.Equals(account.Uuid) && x.Type.Equals(account.Type)).FirstOrDefault();

        if (oldAccount == null)
            throw new Exception("找不到要更新的账户");

        _accounts.Add(account);

        if (isActiveAccount)
            this.ActivateAccount(account);

        RemoveAccount(oldAccount);
    }
}
