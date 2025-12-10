using Application.Common.Models;

namespace Application.Handlers.Ativos.Responses
{
    public static class AtivoErrors
    {
        public static Error NotFound(Guid Id) => new Error(codigo: 404, 
                                                           nome: "Ativo.NotFound", 
                                                           mensagem: "Ativo não encontrado.");

        public static Error CodigoExists(string codigo) => new Error(codigo: 400,
                                                                 nome: "Ativo.CodigoExists",
                                                                 mensagem: $"Já existe um ativo cadastrado com o código '{codigo}'.");
    
    
    }
}
