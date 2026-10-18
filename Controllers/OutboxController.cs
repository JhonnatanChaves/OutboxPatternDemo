using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OutboxPatternDemo.Data;

namespace OutboxPatternDemo.Controllers;

[ApiController]
[Route("api/outbox")]
public class OutboxController : ControllerBase
{
    private readonly AppDbContext _context;

    public OutboxController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var mensagens = await _context.OutboxMessages
            .AsNoTracking()
            .OrderByDescending(m => m.Id)
            .ToListAsync();

        return Ok(mensagens);
    }
}