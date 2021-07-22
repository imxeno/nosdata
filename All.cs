using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NosCDN.NosPack;
using NosCDN.Utils;

namespace NosCDN
{
    public static class All
    {
        [Function("All")]
        public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("All");
            logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            var spark = SparkNosTaleDataSource.Latest();
            var nosGtdDataBytes = spark.FileEntries().Single(e => e.Key.ToLower().Contains("nsgtddata")).Value.Download();
            var nosGtdData = NTStringContainer.Load(nosGtdDataBytes);

            response.WriteString(nosGtdData.Entries.Count + " ");

            return response;
        }
    }
}
