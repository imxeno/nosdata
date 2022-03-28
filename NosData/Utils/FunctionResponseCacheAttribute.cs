using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Net.Http.Headers;

namespace NosData.Utils
{
    internal class FunctionResponseCacheAttribute : FunctionInvocationFilterAttribute
    {
        private readonly int _duration;
        private readonly ResponseCacheLocation _cacheLocation;
        public FunctionResponseCacheAttribute(
            int duration,
            ResponseCacheLocation cacheLocation)
        {
            _duration = duration;
            _cacheLocation = cacheLocation;
        }
        public override async Task OnExecutedAsync(
            FunctionExecutedContext executedContext,
            CancellationToken cancellationToken)
        {
            if (executedContext.Arguments.First().Value is not HttpRequest request)
                throw new ApplicationException(
                    "HttpRequest is null. ModelBinding is not supported, " +
                    "please use HttpRequest as input parameter and deserialize " +
                    "using helper functions.");
            var headers = request.HttpContext.Response.GetTypedHeaders();
            var cacheLocation = executedContext.FunctionResult?.Exception == null
                ? _cacheLocation
                : ResponseCacheLocation.None;
            headers.CacheControl = cacheLocation switch
            {
                ResponseCacheLocation.Any => new CacheControlHeaderValue()
                {
                    MaxAge = TimeSpan.FromSeconds(_duration), NoStore = false, Public = true
                },
                ResponseCacheLocation.Client => new CacheControlHeaderValue()
                {
                    MaxAge = TimeSpan.FromSeconds(_duration), NoStore = false, Public = true
                },
                ResponseCacheLocation.None => new CacheControlHeaderValue() {MaxAge = TimeSpan.Zero, NoStore = true},
                _ => throw new ArgumentOutOfRangeException()
            };
            await base.OnExecutedAsync(executedContext, cancellationToken);
        }
    }
}
