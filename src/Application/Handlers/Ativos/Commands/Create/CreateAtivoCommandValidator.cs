using Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Application.Handlers.Ativos.Commands.Create
{
    public class CreateAtivoCommandValidator : AbstractValidator<CreateAtivoCommand>
    {
        private readonly IApplicationDbContext _context;
        public CreateAtivoCommandValidator(IApplicationDbContext context) 
        {
            _context = context;


            RuleFor(x => x.Codigo)
                .NotEmpty().WithMessage("O código do ativo é obrigatório.")
                .MustAsync(BeUniqueCodigo).WithMessage("Já existe um ativo cadastrado com este código.");


            RuleFor(x => x.Nome)
                .NotEmpty().WithMessage("O nome do ativo é obrigatório.")
                .MaximumLength(100).WithMessage("O nome do ativo não pode exceder 100 caracteres.");
            RuleFor(x => x.PrecoAtual)
                .GreaterThan(0).WithMessage("O preço atual deve ser maior que zero.");
            RuleFor(x => x.RendimentoValorMesAnterior)
                .GreaterThanOrEqualTo(0).WithMessage("O rendimento do mês anterior não pode ser negativo.");
        }

        private async Task<bool> BeUniqueCodigo(string codigo, CancellationToken cancellationToken)
        {
            return !await _context.Ativos.AnyAsync(a => a.Codigo == codigo, cancellationToken);
        }
    }

}
