using Inventory.Business.Validators;
using Inventory.Domain.Contracts;
using Inventory.Domain.DTOs;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;

namespace Inventory.Business;

public class InventarioService : IInventarioService
{
    private readonly IArticuloRepository _repo;
    private readonly ArticuloDtoValidator _validator = new();

    public InventarioService(IArticuloRepository repo) => _repo = repo;

    public async Task<Articulo> InsertarAsync(ArticuloDto dto)
    {
        var val = _validator.Validate(dto);
        if (!val.IsValid)
            throw new DomainException(string.Join(" | ", val.Errors.Select(e => e.ErrorMessage)));

        var existente = await _repo.GetByCodigoAsync(dto.Codigo);
        if (existente is not null)
            throw new DomainException($"Ya existe un artículo con código '{dto.Codigo}'.");

        var art = new Articulo
        {
            Codigo = dto.Codigo,
            Nombre = dto.Nombre,
            CategoriaId = dto.CategoriaId,
            ProveedorId = dto.ProveedorId,
            PrecioCompra = dto.PrecioCompra,
            PrecioVenta = dto.PrecioVenta,
            Stock = dto.Stock,
            StockMin = dto.StockMin
        };

        art.Id = await _repo.InsertAsync(art);

        if (art.Stock < art.StockMin)
            Console.WriteLine($"[ALERTA] Stock bajo para {art.Codigo}");

        return art;
    }

    public Task<Articulo?> ConsultarPorCodigoAsync(string codigo)
        => _repo.GetByCodigoAsync(codigo);

    public Task<IEnumerable<Articulo>> BuscarAsync(string? codigo, string? nombre)
        => _repo.SearchAsync(codigo, nombre);

    public async Task ActualizarAsync(string codigo, ArticuloDto dto)
    {
        var existente = await _repo.GetByCodigoAsync(codigo)
            ?? throw new DomainException($"No existe artículo con código '{codigo}'.");

        var val = _validator.Validate(dto);
        if (!val.IsValid)
            throw new DomainException(string.Join(" | ", val.Errors.Select(e => e.ErrorMessage)));

        existente.Nombre = dto.Nombre;
        existente.CategoriaId = dto.CategoriaId;
        existente.ProveedorId = dto.ProveedorId;
        existente.PrecioCompra = dto.PrecioCompra;
        existente.PrecioVenta = dto.PrecioVenta;
        existente.Stock = dto.Stock;
        existente.StockMin = dto.StockMin;

        await _repo.UpdateAsync(existente);
    }
}
