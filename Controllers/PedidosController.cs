using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OutboxPatternDemo.Data;
using OutboxPatternDemo.DTOs;
using OutboxPatternDemo.Events;
using OutboxPatternDemo.Models;
using System.Text.Json;

namespace OutboxPatternDemo.Controllers;

[ApiController]
[Route("api/pedidos")]
public class PedidosController : ControllerBase
{
    private readonly AppDbContext _context;

    public PedidosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] CriarPedidoDTO dto)
    {
        if (dto.ClienteId <= 0 || dto.Valor <= 0)
            return BadRequest("ClienteId e Valor devem ser maiores que zero.");

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        var pedido = new Pedido
        {
            ClienteId = dto.ClienteId,
            Valor = dto.Valor,
            DataCriacao = DateTime.UtcNow
        };

        _context.Pedidos.Add(pedido);

        await _context.SaveChangesAsync();

        var evento = new PedidoCriadoEvent
        {
            PedidoId = pedido.Id,
            ClienteId = pedido.ClienteId,
            Valor = pedido.Valor,
            DataCriacao = pedido.DataCriacao
        };

        var mensagem = new OutboxMessage
        {
            Type = nameof(PedidoCriadoEvent),
            Content = JsonSerializer.Serialize(evento),
            OccurredOnUtc = DateTime.UtcNow
        };

        _context.OutboxMessages.Add(mensagem);

        await _context.SaveChangesAsync();

        await transaction.CommitAsync();

        return Created($"/api/pedidos/{pedido.Id}", new
        {
            pedido.Id,
            pedido.ClienteId,
            pedido.Valor,
            mensagemId = mensagem.Id
        });
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var pedidos = await _context.Pedidos
            .AsNoTracking()
            .ToListAsync();

        return Ok(pedidos);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> ObterPorId(long id)
    {
        var pedido = await _context.Pedidos
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        return pedido is null ? NotFound() : Ok(pedido);
    }
}