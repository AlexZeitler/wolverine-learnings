using System.Threading.Tasks;
using Alba;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Wolverine;
using Xunit;

namespace WolverineLearnings.Tests;

public class Test
{
  [Fact]
  public async Task hit()
  {
    await using var host = await AlbaHost.For<Program>();

    var account = new Account
    {
      Balance = 1000
    };

    var store = host.Services.GetRequiredService<IDocumentStore>();
    await using var session = store.LightweightSession();
    session.Store(account);
    await session.SaveChangesAsync();

    var bus = host.Services.GetRequiredService<IMessageBus>();
    await bus.InvokeAsync(new DebitAccount(account.Id, 100));

    var account2 = await session.LoadAsync<Account>(account.Id);

    // Should be 1000 + 100
    account2.Balance.ShouldBe(900);
  }
}
