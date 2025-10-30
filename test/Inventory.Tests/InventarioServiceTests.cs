using FluentAssertions;
using Inventory.Business;
using Inventory.Domain.Contracts;
using Inventory.Domain.DTOs;
using Inventory.Domain.Entities;
using Inventory.Domain.Exceptions;
using NSubstitute;
using NUnit.Framework;

namespace Inventory.Tests;

public class InventarioServiceTests
{
    [Test]
    public async Task Insertar_Rechaza_PrecioVenta_Menor_A_Compra()
    {
        var repo = Substitute.For<IArticuloRepository>();
        var svc = new InventarioService(repo);

        var dto = new ArticuloDto
        {
            Codigo = "X1", Nombre = "Perno 1/2",
            CategoriaId = 1, PrecioCompra = 10m, PrecioVenta = 9m,
            Stock = 1, StockMin = 0
        };

        Func<Task> act = async () => await svc.InsertarAsync(dto);
        (await act.Should().ThrowAsync<DomainException>())
           .WithMessage("*venta*compra*");
    }

    [Test]
    public async Task Insertar_Rechaza_Codigo_Duplicado()
    {
        var repo = Substitute.For<IArticuloRepository>();
        repo.GetByCodigoAsync("A-1").Returns(new Articulo { Id = 1, Codigo = "A-1" });
        var svc = new InventarioService(repo);

        var dto = new ArticuloDto
        {
            Codigo = "A-1", Nombre = "Tornillo",
            CategoriaId = 1, PrecioCompra = 1, PrecioVenta = 2, Stock = 10
        };

        Func<Task> act = async () => await svc.InsertarAsync(dto);
        (await act.Should().ThrowAsync<DomainException>())
           .WithMessage("*Ya existe*");
    }
}
