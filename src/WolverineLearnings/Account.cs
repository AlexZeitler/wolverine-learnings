using JasperFx.Core;
using Marten;
using Wolverine;
using Wolverine.Attributes;

namespace WolverineLearnings;

public class Account
{
  public Guid Id { get; set; }
  public decimal Balance { get; set; }
  public decimal MinimumThreshold { get; set; }
}

public interface IAccountCommand
{
  Guid AccountId { get; }
}
  
public record LowBalanceDetected(Guid AccountId) : IAccountCommand;
public record AccountOverdrawn(Guid AccountId) : IAccountCommand;

public record DebitAccount(
  Guid AccountId,
  decimal Amount
) : IAccountCommand;

public record EnforceAccountOverdrawnDeadline(Guid AccountId) : TimeoutMessage(10.Days()), IAccountCommand;
  
// The attribute directs Wolverine to send this message with 
// a "deliver within 5 seconds, or discard" directive
[DeliverWithin(5)]
public record AccountUpdated(Guid AccountId, decimal Balance);

// This is *a* way to build middleware in Wolverine by basically just
// writing functions/methods. There's a naming convention that
// looks for Before/BeforeAsync or After/AfterAsync
public static class AccountLookupMiddleware
{
  // The message *has* to be first in the parameter list
  // Before or BeforeAsync tells Wolverine this method should be called before the actual action
  public static async Task<(HandlerContinuation, Account?)> BeforeAsync(
    IAccountCommand command, 
    ILogger logger, 
    IDocumentSession session, 
    CancellationToken cancellation)
  {
    var account = await session.LoadAsync<Account>(command.AccountId, cancellation);
    if (account == null)
    {
      logger.LogInformation("Unable to find an account for {AccountId}, aborting the requested operation", command.AccountId);
    }
        
    return (account == null ? HandlerContinuation.Stop : HandlerContinuation.Continue, account);
  }
}

public static class DebitAccountHandler
{
  #region sample_DebitAccountHandler_that_uses_IMessageContext

  [Transactional] 
  public static async Task Handle(
    DebitAccount command, 
    Account account, 
    IDocumentSession session, 
    IMessageContext messaging)
  {
    account.Balance -= command.Amount;
     
    // This just marks the account as changed, but
    // doesn't actually commit changes to the database
    // yet. That actually matters as I hopefully explain
    session.Store(account);
 
    // Conditionally trigger other, cascading messages
    if (account.Balance > 0 && account.Balance < account.MinimumThreshold)
    {
      await messaging.SendAsync(new LowBalanceDetected(account.Id));
    }
    else if (account.Balance < 0)
    {
      await messaging.SendAsync(new AccountOverdrawn(account.Id), new DeliveryOptions{DeliverWithin = 1.Hours()});
         
      // Give the customer 10 days to deal with the overdrawn account
      await messaging.ScheduleAsync(new EnforceAccountOverdrawnDeadline(account.Id), 10.Days());
    }
        
    // "messaging" is a Wolverine IMessageContext or IMessageBus service 
    // Do the deliver within rule on individual messages
    await messaging.SendAsync(new AccountUpdated(account.Id, account.Balance),
      new DeliveryOptions { DeliverWithin = 5.Seconds() });
  }

  #endregion
}

