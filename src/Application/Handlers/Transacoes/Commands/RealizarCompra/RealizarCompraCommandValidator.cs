using FluentValidation;

namespace Application.Handlers.Transacoes.Commands.RealizarCompra
{
    public class RealizarCompraCommandValidator : AbstractValidator<RealizarCompraCommand>
    {
        public RealizarCompraCommandValidator()
        {
            RuleFor(x => x.AtivoId)
                .NotEmpty().WithMessage("O ID do ativo é obrigatório.");

            RuleFor(x => x.Quantidade)
                .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");

            RuleFor(x => x.PrecoUnitario)
                .GreaterThan(0).WithMessage("O preço unitário deve ser maior que zero.");

            RuleFor(x => x.Data)
                .NotEmpty().WithMessage("A data da transação é obrigatória.");
        }
    }
}