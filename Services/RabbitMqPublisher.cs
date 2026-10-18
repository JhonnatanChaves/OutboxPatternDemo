using RabbitMQ.Client;
using System.Text;

namespace OutboxPatternDemo.Services;

public class RabbitMqPublisher
{
    private readonly IConfiguration _configuration;

    public RabbitMqPublisher(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Publicar(string mensagem)
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

        channel.ConfirmSelect();

        var body = Encoding.UTF8.GetBytes(mensagem);

        var properties = channel.CreateBasicProperties();
        properties.Persistent = true;

        channel.BasicPublish(
            exchange: "",
            routingKey: queue,
            basicProperties: properties,
            body: body);

        if (!channel.WaitForConfirms(TimeSpan.FromSeconds(5)))
        {
            throw new Exception(
                "O RabbitMQ não confirmou a publicação da mensagem.");
        }
    }
}