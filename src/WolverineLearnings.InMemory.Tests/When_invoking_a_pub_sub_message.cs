using Alba;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shouldly;
using Wolverine;

namespace WolverineLearnings.InMemory.Tests;

public record QuerySync(string Option);

public record QueryASync(string Option);

public record Result(string Value);

public class SyncQueryHandler
{
  public Result Handle(QuerySync querySync) => querySync.Option switch
  {
    "1" => new Result("One"),
    "2" => new Result("Two"),
    _ => throw new ArgumentOutOfRangeException()
  };
}

public class ASyncQueryHandler
{
  public async Task<Result> Handle(QueryASync querySync) => querySync.Option switch
  {
    "1" => await GetDelayedResult(new Result("One")),
    "2" => await GetDelayedResult(new Result("Two")),
    _ => throw new ArgumentOutOfRangeException()
  };

  private async Task<Result> GetDelayedResult(Result result)
  {
    await Task.Delay(5000);
    return result;
  }
}

public class When_invoking_a_request_reply_message_with_a_synchronous_handler : IAsyncLifetime
{
  private IAlbaHost? _host;
  private Result? _result;

  public async Task InitializeAsync()
  {
    _host = await Host
      .CreateDefaultBuilder()
      .UseWolverine()
      .StartAlbaAsync();

    var bus = _host.Services.GetService<IMessageBus>() ?? throw new ArgumentNullException();
    _result = await bus.InvokeAsync<Result>(new QuerySync("1"));
  }

  [Fact]
  public void should_return_reply() => _result.Value.ShouldBe("One");

  public async Task DisposeAsync() => await _host!.DisposeAsync();
}

public class When_invoking_a_request_reply_message_with_an_asynchronous_handler : IAsyncLifetime
{
  private IAlbaHost? _host;
  private Result? _result;

  public async Task InitializeAsync()
  {
    _host = await Host
      .CreateDefaultBuilder()
      .UseWolverine()
      .StartAlbaAsync();

    var bus = _host.Services.GetService<IMessageBus>() ?? throw new ArgumentNullException();
    _result = await bus.InvokeAsync<Result>(new QueryASync("1"));
  }

  [Fact]
  public void should_return_reply() => _result.Value.ShouldBe("One");

  public async Task DisposeAsync() => await _host!.DisposeAsync();
}
