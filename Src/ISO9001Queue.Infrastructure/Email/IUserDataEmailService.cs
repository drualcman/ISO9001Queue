namespace ISO9001Queue.Infrastructure.Email;

public interface IUserDataEmailService
{
    Task SendUserDataAsync(UserDataQueueMessage message, byte[] jsonData, CancellationToken cancellationToken = default);
}
