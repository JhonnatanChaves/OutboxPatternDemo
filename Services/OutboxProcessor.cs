using Microsoft.EntityFrameworkCore;
using OutboxPatternDemo.Data;

namespace OutboxPatternDemo.Services;

public class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox Processor iniciado.");

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ProcessarMensagens(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Erro durante o processamento da Outbox.");
            }
        }
    }

    private async Task ProcessarMensagens(
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<AppDbContext>();

        var publisher = scope.ServiceProvider
            .GetRequiredService<RabbitMqPublisher>();

        var mensagens = await context.OutboxMessages
            .Where(m => m.ProcessedOnUtc == null)
            .OrderBy(m => m.Id)
            .Take(10)
            .ToListAsync(cancellationToken);

        foreach (var mensagem in mensagens)
        {
            try
            {
                publisher.Publicar(mensagem.Content);

                mensagem.ProcessedOnUtc = DateTime.UtcNow;
                mensagem.Error = null;

                await context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Mensagem Outbox {Id} publicada com sucesso.",
                    mensagem.Id);
            }
            catch (Exception ex)
            {
                mensagem.Error = ex.Message;

                await context.SaveChangesAsync(cancellationToken);

                _logger.LogError(
                    ex,
                    "Falha ao publicar mensagem Outbox {Id}.",
                    mensagem.Id);
            }
        }
    }
}