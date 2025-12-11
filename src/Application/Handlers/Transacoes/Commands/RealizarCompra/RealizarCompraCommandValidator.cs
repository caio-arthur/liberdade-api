using Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers.Transacoes.Commands.RealizarCompra
{
    public class RealizarCompraCommandValidator : AbstractValidator<RealizarCompraCommand>
    {
        private readonly IApplicationDbContext _context;

        public RealizarCompraCommandValidator(IApplicationDbContext context)
        {
            _context = context;

            RuleFor(x => x.AtivoCodigo)
                .NotEmpty().WithMessage("O Codigo do ativo é obrigatório.")
                .MustAsync(AtivoExists).WithMessage("O ativo com o código especificado não existe.")
                ;
                    

            RuleFor(x => x.Quantidade)
                .GreaterThan(0).WithMessage("A quantidade deve ser maior que zero.");

            RuleFor(x => x.PrecoUnitario)
                .GreaterThan(0).WithMessage("O preço unitário deve ser maior que zero.");

            RuleFor(x => x.Data)
                .NotEmpty().WithMessage("A data da transação é obrigatória.");
        }

        private async Task<bool> AtivoExists(string ativoCodigo, CancellationToken cancellationToken)
        {
            var exists = await _context.Ativos.AnyAsync(a => a.Codigo == ativoCodigo, cancellationToken);
            return exists;
        }
    }
}