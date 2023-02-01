using System;
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
  public async Task the_application_assembly_is_inferred_correctly()
  {
    #region sample_disabling_the_transports_from_web_application_factory

    // This is using Alba to bootstrap a Wolverine application
    // for integration tests, but it's using WebApplicationFactory
    // to do the actual bootstrapping
    await using var host = await AlbaHost.For<Program>(x =>
    {
      // I'm overriding 
      x.ConfigureServices(services => services.DisableAllExternalWolverineTransports());
    });

    #endregion
        
        
    var options = host.Services.GetRequiredService<WolverineOptions>();
    options.ApplicationAssembly.ShouldBe(typeof(Account).Assembly);
  }
  
  [Fact]
  public async Task validation_miss()
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

    await Should.ThrowAsync<Exception>(async () =>
    {
      await bus.InvokeAsync(new DebitAccount(account.Id, 0));
    });
  }


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
