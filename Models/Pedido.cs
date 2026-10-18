namespace OutboxPatternDemo.Models;

public class Pedido
{
    public long Id { get; set; }
    public long ClienteId { get; set; }
    public decimal Valor { get; set; }
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
}