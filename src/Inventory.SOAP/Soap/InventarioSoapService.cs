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
        private readonly IArticuloRepository _repo;

        public InventarioSoapService(IInventarioService svc, IArticuloRepository repo)
        {
            _svc = svc ?? throw new ArgumentNullException(nameof(svc));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public ArticuloSoap InsertarArticulo(ArticuloInputSoap input)
        {
            if (input == null)
                throw Fault("El cuerpo de la solicitud es nulo o inválido.");

            try
            {
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
                throw Fault(ex.Message);
            }
            catch (Exception ex)
            {
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

        public void ActualizarArticulo(string codigo, ArticuloInputSoap input)
        {
            var dto = new ArticuloDto
            {
                Codigo = codigo,
                Nombre = input.Nombre,
                CategoriaId = input.CategoriaId,
                ProveedorId = input.ProveedorId,
                PrecioCompra = input.PrecioCompra,
                PrecioVenta = input.PrecioVenta,
                Stock = input.Stock,
                StockMin = input.StockMin
            };

            _svc.ActualizarAsync(codigo, dto).GetAwaiter().GetResult();
        }

        public void EliminarArticulo(string codigo)
        {
            var art = _repo.GetByCodigoAsync(codigo).GetAwaiter().GetResult()
                ?? throw new FaultException($"El artículo con código '{codigo}' no existe.");

            _repo.DeleteAsync(art.Id).GetAwaiter().GetResult();
        }

        public IEnumerable<ArticuloSoap> BuscarArticulos(string? codigo, string? nombre)
        {
            var articulos = _svc.BuscarAsync(codigo, nombre).GetAwaiter().GetResult();
            return articulos.Select(a => new ArticuloSoap
            {
                Id = a.Id,
                Codigo = a.Codigo,
                Nombre = a.Nombre,
                CategoriaId = a.CategoriaId,
                ProveedorId = a.ProveedorId,
                PrecioCompra = a.PrecioCompra,
                PrecioVenta = a.PrecioVenta,
                Stock = a.Stock,
                StockMin = a.StockMin
            }).ToList();
        }

        private static FaultException Fault(string message) =>
            new FaultException(message);
    }
}