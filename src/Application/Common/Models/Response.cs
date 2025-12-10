namespace Application.Common.Models
{
    public class Response 
    {
        public bool Sucesso { get; }
        public Error Erro { get; }

        protected Response(bool success, Error error)
        {
            Sucesso = success;
            Erro = error;
        }

        public static Response Failure(Error error)
        {
            return new Response(false, error);
        }

        public static Response Success()
        {
            return new Response(true, null);
        }

        public static Response<TData> Failure<TData>(Error error)
        {
            return new Response<TData>(false, default, error);
        }

        public static Response<TData> Success<TData>(TData value = default)
        {
            return new Response<TData>(true, value, null);
        }
    }

    public class Response<TData> : Response
    {
        public TData Dados { get; }

        public Response(bool isSuccess, TData data, Error error) : base(isSuccess, error)
        {
            Dados = data;
        }
    }
}
