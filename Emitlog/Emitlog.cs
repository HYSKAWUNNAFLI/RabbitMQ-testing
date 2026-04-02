using RabbitMQ.Client;
using System.Text;

var factory = new ConnectionFactory { HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "localhost" };
using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();

await channel.ExchangeDeclareAsync(exchange: "logs", type: ExchangeType.Fanout);

Console.WriteLine(" [*] Emitting logs... Press Stop to exit.");
int count = 0;
while (true)
{
    var msg = $"{GetMessage(args)} #{++count}";
    var body = Encoding.UTF8.GetBytes(msg);
    await channel.BasicPublishAsync(exchange: "logs", routingKey: string.Empty, body: body);
    Console.WriteLine($" [x] Sent {msg}");
    await Task.Delay(2000);
}

static string GetMessage(string[] args)
{
    return ((args.Length > 0) ? string.Join(" ", args) : "info: Hello World!");
}