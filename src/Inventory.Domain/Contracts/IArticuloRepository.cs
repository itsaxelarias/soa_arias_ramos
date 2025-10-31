using System.Collections.Generic;
using System.Threading.Tasks;
using Inventory.Domain.Entities;

namespace Inventory.Domain.Contracts
{
    public interface IArticuloRepository
    {
        Task<int> InsertAsync(Articulo entidad);
        Task<int> UpdateAsync(Articulo entidad);
        Task DeleteAsync(int id);

        Task<Articulo?> GetByCodigoAsync(string codigo);

        Task<IEnumerable<Articulo>> SearchAsync(string? codigo, string? nombre);
    }
}