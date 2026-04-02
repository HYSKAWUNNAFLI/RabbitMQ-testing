using RabbitMQ.Client;
using System.Text;

var factory = new ConnectionFactory { HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync(queue: "task_queue", durable: true, exclusive: false,
    autoDelete: false, arguments: new Dictionary<string, object?> { { "x-queue-type", "quorum" } });

Console.WriteLine(" [*] Sending tasks... Press Stop to exit.");
int count = 0;
while (true)
{
    var msg = $"{GetMessage(args)} #{++count}";
    var body = Encoding.UTF8.GetBytes(msg);
    var properties = new BasicProperties { Persistent = true };
    await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "task_queue", mandatory: true,
        basicProperties: properties, body: body);
    Console.WriteLine($" [x] Sent {msg}");
    await Task.Delay(2000);
}

static string GetMessage(string[] args)
{
    return ((args.Length > 0) ? string.Join(" ", args) : "Hello World!");
}