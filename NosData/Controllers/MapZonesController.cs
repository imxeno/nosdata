using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NosData.Utils;

namespace NosData.Controllers
{
    public class MapZonesController
    {
        private readonly MapZonesService _mapZonesService;

        public MapZonesController(MapZonesService mapZonesService)
        {
            _mapZonesService = mapZonesService;
        }

        [FunctionName("GetMapZone")]
        [FunctionResponseCache(60 * 60, ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetMapZone(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "mapZones/{id:int}")] HttpRequest req,
            ILogger log, int id)
        {
            var zone = await _mapZonesService.GetMapZone(id);
            if (zone == null) return new NotFoundResult();
            return new FileStreamResult(zone, "application/json");
        }
    }
}
