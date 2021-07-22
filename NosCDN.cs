using System.Linq;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using NosCDN.Converter;
using NosCDN.NosPack;
using NosCDN.Utils;

namespace NosCDN
{
    public static class NosCDN
    {
        [Function("Data/{file}")]
        public static HttpResponseData Run([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
            FunctionContext executionContext, string file)
        {
            var logger = executionContext.GetLogger("NosCDN");
            logger.LogInformation("C# HTTP trigger function processed a request.");

            var spark = SparkNosTaleDataSource.Latest();
            var nosGtdDataBytes = spark.FileEntries().Single(e => e.Key.ToLower().Contains("nsgtddata")).Value.Download();
            var nosGtdData = NTStringContainer.Load(nosGtdDataBytes);

            if (!nosGtdData.Entries.TryGetValue(file, out var datFile))
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.NotFound);
                return errorResponse;
            }

            var itemDat = System.Text.Encoding.ASCII.GetString(datFile.Content);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            response.WriteString(NosTaleDatToJsonConverter.Convert(itemDat).ToString());

            return response;
        }
    }
}
