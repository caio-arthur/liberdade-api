namespace Application.Common.Models
{
    public static class Response 
    {
        public static Response<TData> Failure<TData>(Error error)
        {
            return new Response<TData>(false, default, error);
        }

        public static Response<TData> Success<TData>(TData value = default)
        {
            return new Response<TData>(true, value, null);
        }
    }

    public class Response<TData>
    {
        public bool Sucesso { get; }
        public TData Dados { get; }
        public Error Erro { get; }
        public Response(bool isSuccess, TData data, Error error)
        {
            Sucesso = isSuccess;
            Dados = data;
            Erro = error;
        }
    }
}
