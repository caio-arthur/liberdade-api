namespace Application.Common.Models
{
    public class Error
    {
        public Error(int codigo, string nome, string mensagem)
        {
            Codigo = codigo;
            Nome = nome;
            Mensagem = mensagem;
        }

        public int Codigo { get; set; }
        public string Nome { get; set; }
        public string Mensagem { get; set; }
    }
}