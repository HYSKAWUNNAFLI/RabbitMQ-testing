using RabbitMQ.Client;
using System.Text;

var factory = new ConnectionFactory { HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.QueueDeclareAsync(queue: "hello", durable: true, exclusive: false, autoDelete: false,
    arguments: new Dictionary<string, object?> { { "x-queue-type", "quorum" } });

const string message = "Hello World!";
Console.WriteLine(" [*] Sending messages... Press Stop to exit.");
int count = 0;
while (true)
{
    var msg = $"{message} #{++count}";
    var updatedBody = Encoding.UTF8.GetBytes(msg);
    await channel.BasicPublishAsync(exchange: string.Empty, routingKey: "hello", body: updatedBody);
    Console.WriteLine($" [x] Sent {msg}");
    await Task.Delay(2000);
}