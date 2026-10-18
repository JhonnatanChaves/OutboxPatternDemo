namespace OutboxPatternDemo.Events;

public class PedidoCriadoEvent
{
    public long PedidoId { get; set; }
    public long ClienteId { get; set; }
    public decimal Valor { get; set; }
    public DateTime DataCriacao { get; set; }
}