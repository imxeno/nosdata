using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NosData.Services;

namespace NosData.Controllers
{
    public class DataController
    {
        private readonly DataService _dataService;

        public DataController(DataService dataService)
        {
            _dataService = dataService;
        }

        [FunctionName("GetData")]
        public async Task<IActionResult> GetData(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "data/{type}")] HttpRequest req,
            ILogger log, string type)
        {
            if (!DataService.GenericDatFiles.ContainsKey(type))
            {
                return new NotFoundResult();
            }
            var data = await _dataService.GetData(type);
            if (data == null) return new StatusCodeResult(503);
            return new OkObjectResult(data);
        }

        [FunctionName("GetRawData")]
        public async Task<IActionResult> GetRawData(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "data/raw/{type}")] HttpRequest req,
            ILogger log, string type)
        {
            if (!DataService.GenericDatFiles.ContainsKey(type) && !DataService.RawOnlyDatFiles.ContainsKey(type))
            {
                return new NotFoundResult();
            }
            var data = await _dataService.GetRawData(type);
            if (data == null) return new StatusCodeResult(503);
            return new FileContentResult(data, "text/plain");
        }

    }
}
