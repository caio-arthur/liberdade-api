using FluentValidation;

namespace Application.Handlers.Transacoes.Commands.RegistrarAporte
{
    public class RegistrarAporteCommandValidator : AbstractValidator<RegistrarAporteCommand>
    {
        public RegistrarAporteCommandValidator()
        {
            RuleFor(x => x.Valor)
                .GreaterThan(0).WithMessage("O valor do aporte deve ser maior que zero.");

            RuleFor(x => x.Data)
                .NotEmpty().WithMessage("A data da transação é obrigatória.");
        }
    }
}