using FluentValidation;
using Inventory.Domain.DTOs;

namespace Inventory.Business.Validators;

public class ArticuloDtoValidator : AbstractValidator<ArticuloDto>
{
    public ArticuloDtoValidator()
    {
        RuleFor(x => x.Codigo).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Nombre).NotEmpty().MaximumLength(120);
        RuleFor(x => x.CategoriaId).GreaterThan(0);
        RuleFor(x => x.PrecioCompra).GreaterThanOrEqualTo(0);
        RuleFor(x => x.PrecioVenta).GreaterThanOrEqualTo(0)
            .Must((dto, pv) => pv >= dto.PrecioCompra)
            .WithMessage("El precio de venta debe ser mayor o igual al precio de compra.");
        RuleFor(x => x.Stock).GreaterThanOrEqualTo(0);
        RuleFor(x => x.StockMin).GreaterThanOrEqualTo(0);
    }
}
