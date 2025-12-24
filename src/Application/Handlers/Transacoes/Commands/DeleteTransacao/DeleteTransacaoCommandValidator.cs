using FluentValidation;

namespace Application.Handlers.Transacoes.Commands.DeleteTransacao
{
    public class DeleteTransacaoCommandValidator : AbstractValidator<DeleteTransacaoCommand>
    {
        public DeleteTransacaoCommandValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("O Id da transação é obrigatório.");
        }
    }
}
