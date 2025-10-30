using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using Inventory.Domain.Soap;
using Inventory.Domain.DTOs;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;
using Inventory.Domain.Contracts;

namespace Inventory.ServiceHost.Soap
{
    public class InventarioSoapService : IInventarioSoap
    {
        private readonly IInventarioService _svc;

        public InventarioSoapService(IInventarioService svc)
        {
            _svc = svc ?? throw new ArgumentNullException(nameof(svc));
        }

        public ArticuloSoap InsertarArticulo(ArticuloInputSoap input)
        {
            if (input == null)
                throw Fault("El cuerpo de la solicitud es nulo o inválido.");

            try
            {
                // Mapeo: SOAP → DTO
                var dto = new ArticuloDto
                {
                    Codigo       = input.Codigo ?? string.Empty,
                    Nombre       = input.Nombre ?? string.Empty,
                    CategoriaId  = input.CategoriaId,
                    ProveedorId  = input.ProveedorId,
                    PrecioCompra = input.PrecioCompra,
                    PrecioVenta  = input.PrecioVenta,
                    Stock        = input.Stock,
                    StockMin     = input.StockMin
                };

                var creado = _svc.InsertarAsync(dto).GetAwaiter().GetResult();

                return new ArticuloSoap
                {
                    Id           = creado.Id,
                    Codigo       = creado.Codigo,
                    Nombre       = creado.Nombre,
                    CategoriaId  = creado.CategoriaId,
                    ProveedorId  = creado.ProveedorId,
                    PrecioCompra = creado.PrecioCompra,
                    PrecioVenta  = creado.PrecioVenta,
                    Stock        = creado.Stock,
                    StockMin     = creado.StockMin
                };
            }
            catch (DomainException ex)
            {
                // Error de validación de negocio (regla incumplida, datos inválidos)
                throw Fault(ex.Message);
            }
            catch (Exception ex)
            {
                // Error inesperado (DB, nulos, etc.)
                Console.WriteLine($"[SOAP ERROR] {ex}");
                throw Fault("Error interno en el servidor.");
            }
        }

        public ArticuloSoap? ConsultarArticuloPorCodigo(string codigo)
        {
            var art = _svc.ConsultarPorCodigoAsync(codigo).GetAwaiter().GetResult();
            if (art is null) return null;

            return new ArticuloSoap
            {
                Id           = art.Id,
                Codigo       = art.Codigo,
                Nombre       = art.Nombre,
                CategoriaId  = art.CategoriaId,
                ProveedorId  = art.ProveedorId,
                PrecioCompra = art.PrecioCompra,
                PrecioVenta  = art.PrecioVenta,
                Stock        = art.Stock,
                StockMin     = art.StockMin
            };
        }

        public IEnumerable<ArticuloSoap> ListarArticulos(int limit)
        {
            var lista = _svc.BuscarAsync(null, null).GetAwaiter().GetResult();
            return lista
                .Take(limit)
                .Select(a => new ArticuloSoap
                {
                    Id           = a.Id,
                    Codigo       = a.Codigo,
                    Nombre       = a.Nombre,
                    CategoriaId  = a.CategoriaId,
                    ProveedorId  = a.ProveedorId,
                    PrecioCompra = a.PrecioCompra,
                    PrecioVenta  = a.PrecioVenta,
                    Stock        = a.Stock,
                    StockMin     = a.StockMin
                })
                .ToList();
        }

        private static FaultException Fault(string message) =>
            new FaultException(message);
    }
}
