namespace Inventory.Domain.Entities;

public class Articulo
{
    public int Id { get; set; }
    public string Codigo { get; set; } = default!;
    public string Nombre { get; set; } = default!;
    public int CategoriaId { get; set; }
    public int? ProveedorId { get; set; }
    public decimal PrecioCompra { get; set; }
    public decimal PrecioVenta { get; set; }
    public int Stock { get; set; }
    public int StockMin { get; set; }
    public DateTime CreadoEn { get; set; }
}
