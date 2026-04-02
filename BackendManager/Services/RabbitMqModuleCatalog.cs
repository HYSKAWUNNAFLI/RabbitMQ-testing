using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace BackendManager.Services
{
    public sealed class RabbitMqModuleCatalog
    {
        private static readonly HashSet<string> SupportedModules = new(StringComparer.OrdinalIgnoreCase)
        {
            "NewTask",
            "Worker",
            "Send",
            "Receive",
            "Emitlog",
            "Receivelogs"
        };

        private readonly IConfiguration _configuration;

        public RabbitMqModuleCatalog(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool IsSupported(string moduleName)
        {
            return SupportedModules.Contains(moduleName);
        }

        public Task RunModuleAsync(string moduleName, ModuleExecutionContext context)
        {
            return moduleName switch
            {
                "NewTask" => RunNewTaskAsync(context),
                "Worker" => RunWorkerAsync(context),
                "Send" => RunSendAsync(context),
                "Receive" => RunReceiveAsync(context),
                "Emitlog" => RunEmitlogAsync(context),
                "Receivelogs" => RunReceivelogsAsync(context),
                _ => throw new InvalidOperationException($"Module {moduleName} is not supported.")
            };
        }

        private async Task RunNewTaskAsync(ModuleExecutionContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var factory = CreateConnectionFactory();
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: "task_queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: CreateQuorumQueueArguments());

            await context.WriteLogAsync(" [*] Sending tasks... Press Stop to exit.");

            var count = 0;
            while (true)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var message = $"Hello World! #{++count}";
                var body = Encoding.UTF8.GetBytes(message);
                var properties = new BasicProperties { Persistent = true };

                await channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: "task_queue",
                    mandatory: true,
                    basicProperties: properties,
                    body: body);

                await context.WriteLogAsync($" [x] Sent {message}");
                await Task.Delay(TimeSpan.FromSeconds(2), context.CancellationToken);
            }
        }

        private async Task RunWorkerAsync(ModuleExecutionContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var factory = CreateConnectionFactory();
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: "task_queue",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: CreateQuorumQueueArguments());

            await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);
            await context.WriteLogAsync(" [*] Waiting for messages.");

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                await context.WriteLogAsync($" [x] Received {message}");

                try
                {
                    var dots = message.Split('.').Length - 1;
                    if (dots > 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(dots), context.CancellationToken);
                    }

                    await context.WriteLogAsync(" [x] Done");
                    await channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                }
                catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
                {
                    await TryRequeueAsync(channel, ea.DeliveryTag);
                }
                catch (Exception ex)
                {
                    await context.WriteLogAsync($"[ERROR] Worker failed: {ex.Message}");
                    await TryRequeueAsync(channel, ea.DeliveryTag);
                }
            };

            var consumerTag = await channel.BasicConsumeAsync(
                queue: "task_queue",
                autoAck: false,
                consumer: consumer);

            await context.WriteLogAsync(" [*] Listening... Press Stop in Web UI to exit.");

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, context.CancellationToken);
            }
            finally
            {
                await TryCancelConsumerAsync(channel, consumerTag);
            }
        }

        private async Task RunSendAsync(ModuleExecutionContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var factory = CreateConnectionFactory();
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: "hello",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: CreateQuorumQueueArguments());

            await context.WriteLogAsync(" [*] Sending messages... Press Stop to exit.");

            var count = 0;
            while (true)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var message = $"Hello World! #{++count}";
                var body = Encoding.UTF8.GetBytes(message);

                await channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: "hello",
                    body: body);

                await context.WriteLogAsync($" [x] Sent {message}");
                await Task.Delay(TimeSpan.FromSeconds(2), context.CancellationToken);
            }
        }

        private async Task RunReceiveAsync(ModuleExecutionContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var factory = CreateConnectionFactory();
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: "hello",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: CreateQuorumQueueArguments());

            await context.WriteLogAsync(" [*] Waiting for messages.");

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                await context.WriteLogAsync($" [x] Received {message}");
            };

            var consumerTag = await channel.BasicConsumeAsync(
                queue: "hello",
                autoAck: true,
                consumer: consumer);

            await context.WriteLogAsync(" [*] Listening... Press Stop in Web UI to exit.");

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, context.CancellationToken);
            }
            finally
            {
                await TryCancelConsumerAsync(channel, consumerTag);
            }
        }

        private async Task RunEmitlogAsync(ModuleExecutionContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var factory = CreateConnectionFactory();
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(exchange: "logs", type: ExchangeType.Fanout);
            await context.WriteLogAsync(" [*] Emitting logs... Press Stop to exit.");

            var count = 0;
            while (true)
            {
                context.CancellationToken.ThrowIfCancellationRequested();

                var message = $"info: Hello World! #{++count}";
                var body = Encoding.UTF8.GetBytes(message);

                await channel.BasicPublishAsync(
                    exchange: "logs",
                    routingKey: string.Empty,
                    body: body);

                await context.WriteLogAsync($" [x] Sent {message}");
                await Task.Delay(TimeSpan.FromSeconds(2), context.CancellationToken);
            }
        }

        private async Task RunReceivelogsAsync(ModuleExecutionContext context)
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var factory = CreateConnectionFactory();
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.ExchangeDeclareAsync(exchange: "logs", type: ExchangeType.Fanout);

            var queueDeclareResult = await channel.QueueDeclareAsync();
            var queueName = queueDeclareResult.QueueName;
            await channel.QueueBindAsync(queue: queueName, exchange: "logs", routingKey: string.Empty);

            await context.WriteLogAsync(" [*] Waiting for logs.");

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (_, ea) =>
            {
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                await context.WriteLogAsync($" [x] {message}");
            };

            var consumerTag = await channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: true,
                consumer: consumer);

            await context.WriteLogAsync(" [*] Listening... Press Stop in Web UI to exit.");

            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, context.CancellationToken);
            }
            finally
            {
                await TryCancelConsumerAsync(channel, consumerTag);
            }
        }

        private ConnectionFactory CreateConnectionFactory()
        {
            return new ConnectionFactory
            {
                HostName = _configuration["RABBITMQ_HOST"] ?? "localhost",
                UserName = _configuration["RABBITMQ_USERNAME"] ?? "guest",
                Password = _configuration["RABBITMQ_PASSWORD"] ?? "guest",
                AutomaticRecoveryEnabled = true,
                TopologyRecoveryEnabled = true
            };
        }

        private static Dictionary<string, object?> CreateQuorumQueueArguments()
        {
            return new Dictionary<string, object?> { { "x-queue-type", "quorum" } };
        }

        private static async Task TryCancelConsumerAsync(IChannel channel, string consumerTag)
        {
            try
            {
                await channel.BasicCancelAsync(consumerTag);
            }
            catch
            {
            }
        }

        private static async Task TryRequeueAsync(IChannel channel, ulong deliveryTag)
        {
            try
            {
                await channel.BasicNackAsync(deliveryTag: deliveryTag, multiple: false, requeue: true);
            }
            catch
            {
            }
        }
    }
}
