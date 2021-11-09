using System.Threading;
using System.Threading.Tasks;

namespace ShitpostBot.Domain
{
    public interface IDomainRequest
    {
    }

    public interface IDomainRequest<T> : IDomainRequest
    {
    }

    public interface IHandler<TRequest> where TRequest : IDomainRequest
    {
        Task Handle(TRequest request, CancellationToken cancellationToken);
    }
    
    public interface IHandler<TRequest, TResponse> where TRequest : IDomainRequest<TResponse>
    {
        Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
    }

    public class Response<T> where T : class
    {
        public bool IsSuccessful { get; private set; }
        public T Payload { get; private set; }

        public Response(bool isSuccessful, T payload)
        {
            IsSuccessful = isSuccessful;
            Payload = payload;
        }

        public static Response<T> Success(T payload)
        {
            return new Response<T>(true, payload);
        }

        public static Response<T> Error()
        {
            return new Response<T>(false, (T) null);
        }
    }
}