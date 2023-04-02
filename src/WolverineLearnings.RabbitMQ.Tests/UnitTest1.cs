using Alba;
using Microsoft.Extensions.Hosting;
using Oakton.Resources;
using Wolverine;
using Wolverine.RabbitMQ;

namespace WolverineLearnings.RabbitMQ.Tests;

public class UnitTest1 : IAsyncLifetime
{
  private IAlbaHost? _host;

  public async Task InitializeAsync()
  {
    var builder = Host.CreateDefaultBuilder();
    builder.UseWolverine(
      options =>
      {
        options.PublishMessage<CreateIssue>()
          .ToRabbitExchange("issues");
        options
          .ListenToRabbitQueue("issues");

        options.UseRabbitMq(rabbit => { rabbit.HostName = "localhost"; })
          .BindExchange("issues")
          .ToQueue("issues")
          .AutoProvision();

        options.Services.AddResourceSetupOnStartup();
      }
    );

    _host = await builder.StartAlbaAsync();
  }
  
  [Fact]
  public void Test1()
  {
   
  }

 

  public async Task DisposeAsync() => await _host.DisposeAsync();
}

public record CreateIssue;
