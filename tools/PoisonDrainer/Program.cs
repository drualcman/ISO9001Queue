using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;

// Moves every message from each *-poison queue back to its original queue.
// Usage:  dotnet run -- "<storage-connection-string>"
//   (or set the env var BLOB and run:  dotnet run)

string? connectionString =
    args.Length > 0 ? args[0] : Environment.GetEnvironmentVariable("BLOB");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine("Falta la cadena de conexion. Uso: dotnet run -- \"<connection-string>\"");
    return 1;
}

string[] poisonQueues =
[
    "iso9001-auditlogs-poison",
    "iso9001-incidents-poison",
    "iso9001-feedbacks-poison",
    "iso9001-nonconformities-poison",
    "iso9001-userdata-poison",
];

int total = 0;
foreach (string poisonName in poisonQueues)
{
    var source = new QueueClient(connectionString, poisonName);
    if (!await source.ExistsAsync())
    {
        Console.WriteLine($"[skip] {poisonName} no existe.");
        continue;
    }

    string targetName = poisonName[..^"-poison".Length];
    var target = new QueueClient(connectionString, targetName);
    await target.CreateIfNotExistsAsync();

    int moved = 0;
    while (true)
    {
        // Long visibility so the same batch is not re-received while we process it.
        QueueMessage[] messages = (await source.ReceiveMessagesAsync(
            maxMessages: 32,
            visibilityTimeout: TimeSpan.FromMinutes(5))).Value;

        if (messages.Length == 0)
            break;

        foreach (QueueMessage message in messages)
        {
            // Body is the raw stored payload; re-send it verbatim (no re-encoding).
            await target.SendMessageAsync(message.Body);
            await source.DeleteMessageAsync(message.MessageId, message.PopReceipt);
            moved++;
        }
        Console.WriteLine($"  {poisonName}: {moved} movidos...");
    }

    Console.WriteLine($"[ok] {poisonName} -> {targetName}: {moved} mensajes.");
    total += moved;
}

Console.WriteLine($"Hecho. Total reenviado: {total}.");
return 0;
