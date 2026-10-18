using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace OutboxPatternDemo.Services;

public class RabbitMqConsumer : BackgroundService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqConsumer> _logger;

    public RabbitMqConsumer(
        IConfiguration configuration,
        ILogger<RabbitMqConsumer> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumirMensagens(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro na conexão com RabbitMQ. Tentando novamente...");

                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task ConsumirMensagens(
        CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMq:Host"],
            Port = _configuration.GetValue<int>("RabbitMq:Port"),
            UserName = _configuration["RabbitMq:Username"],
            Password = _configuration["RabbitMq:Password"]
        };

        using var connection = factory.CreateConnection();
        using var channel = connection.CreateModel();

        var queue = _configuration["RabbitMq:Queue"]!;

        channel.QueueDeclare(
            queue: queue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        channel.BasicQos(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false);

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += (_, args) =>
        {
            var mensagem = Encoding.UTF8.GetString(
                args.Body.ToArray());

            try
            {
                _logger.LogInformation(
                    "Evento recebido do RabbitMQ: {Mensagem}",
                    mensagem);

                // Aqui entraria a regra de negócio do consumidor.

                channel.BasicAck(
                    deliveryTag: args.DeliveryTag,
                    multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro ao processar mensagem RabbitMQ.");

                channel.BasicNack(
                    deliveryTag: args.DeliveryTag,
                    multiple: false,
                    requeue: true);
            }
        };

        channel.BasicConsume(
            queue: queue,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation(
            "Consumidor RabbitMQ iniciado. Aguardando mensagens...");

        try
        {
            await Task.Delay(
                Timeout.Infinite,
                stoppingToken);
        }
        finally
        {
            if (channel.IsOpen)
            {
                channel.Close();
            }
        }
    }
}