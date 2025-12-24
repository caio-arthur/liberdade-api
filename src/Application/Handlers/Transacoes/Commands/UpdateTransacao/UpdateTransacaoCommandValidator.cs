using FluentValidation;

namespace Application.Handlers.Transacoes.Commands.UpdateTransacao
{
    public class UpdateTransacaoCommandValidator : AbstractValidator<UpdateTransacaoCommand>
    {
        public UpdateTransacaoCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("O Id da transação é obrigatório.");

            RuleFor(x => x.Quantidade)
                .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");

            RuleFor(x => x.PrecoUnitario)
                .GreaterThan(0).WithMessage("O preço unitário deve ser maior que zero.");

            RuleFor(x => x.Data)
                .NotEmpty().WithMessage("A data da transação é obrigatória.");
        }
    }
}
