namespace Inventory.Domain.DTOs;

public class ArticuloDto
{
    public string Codigo { get; set; } = default!;
    public string Nombre { get; set; } = default!;
    public int CategoriaId { get; set; }
    public int? ProveedorId { get; set; }
    public decimal PrecioCompra { get; set; }
    public decimal PrecioVenta { get; set; }
    public int Stock { get; set; }
    public int StockMin { get; set; } = 0;
}
