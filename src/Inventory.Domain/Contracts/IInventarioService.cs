using Inventory.Domain.DTOs;
using Inventory.Domain.Entities;

namespace Inventory.Domain.Contracts
{
    public interface IInventarioService
    {
        /// <summary>
        /// Inserta un nuevo artículo en el inventario.
        /// </summary>
        Task<Articulo> InsertarAsync(ArticuloDto dto);

        /// <summary>
        /// Consulta un artículo por su código único.
        /// </summary>
        Task<Articulo?> ConsultarPorCodigoAsync(string codigo);

        /// <summary>
        /// Busca artículos por código o nombre.
        /// </summary>
        Task<IEnumerable<Articulo>> BuscarAsync(string? codigo, string? nombre);

        /// <summary>
        /// Actualiza un artículo existente según su código.
        /// </summary>
        Task ActualizarAsync(string codigo, ArticuloDto dto);
    }
}
