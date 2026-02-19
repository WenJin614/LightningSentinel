using Grpc.Core;
using Grpc.Core.Interceptors;

namespace LightningProbe.Interceptor
{
    public class MacaroonInterceptor : Grpc.Core.Interceptors.Interceptor
    {
        private readonly string _macaroonHex;
        public MacaroonInterceptor(string hex) => _macaroonHex = hex;

        // This handles standard "one-shot" calls (like QueryRoutes)
        public override AsyncUnaryCall<TResponse> AsyncUnaryCall<TRequest, TResponse>(
            TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
            AsyncUnaryCallContinuation<TRequest, TResponse> continuation)
        {
            return continuation(request, AddMacaroon(context));
        }

        // THIS IS NEW: This handles streaming calls (like SendPaymentV2/Probing)
        public override AsyncServerStreamingCall<TResponse> AsyncServerStreamingCall<TRequest, TResponse>(
            TRequest request, ClientInterceptorContext<TRequest, TResponse> context,
            AsyncServerStreamingCallContinuation<TRequest, TResponse> continuation)
        {
            return continuation(request, AddMacaroon(context));
        }

        private ClientInterceptorContext<TRequest, TResponse> AddMacaroon<TRequest, TResponse>(
            ClientInterceptorContext<TRequest, TResponse> context)
            where TRequest : class where TResponse : class
        {
            var headers = context.Options.Headers ?? new Metadata();
            headers.Add("macaroon", _macaroonHex);
            return new ClientInterceptorContext<TRequest, TResponse>(
                context.Method, context.Host, context.Options.WithHeaders(headers));
        }
    }
}
