using System.Collections.Generic;
using System.Threading.Tasks;
using Inventory.Domain.Entities;

namespace Inventory.Domain.Contracts
{
    public interface IArticuloRepository
    {
        Task<int> InsertAsync(Articulo entidad);
        Task<int> UpdateAsync(Articulo entidad);

        Task<Articulo?> GetByCodigoAsync(string codigo);

        // Usado por el listado rápido en SOAP:
        Task<IEnumerable<Articulo>> SearchAsync(string? codigo, string? nombre);

        // Si prefieres, puedes tener también:
        // Task<IEnumerable<Articulo>> GetAllAsync();
    }
}
