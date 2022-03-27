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

namespace NosData.Controllers
{
    public class IconsController
    {
        private readonly IconsService _iconsService;

        public IconsController(IconsService iconsService)
        {
            _iconsService = iconsService;
        }

        [FunctionName("GetAllIcons")]
        public async Task<IActionResult> GetAllIcons(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "icons")] HttpRequest req,
            ILogger log)
        {
            try
            {
                var icon = await _iconsService.GetAllIcons();
                if (icon == null) return new StatusCodeResult(503);
                return new OkObjectResult(icon);
            }
            catch (Exception e)
            {
                log.LogCritical(e.Message);
                throw;
            }
        }

        [FunctionName("GetIconsSpriteSheet")]
        public async Task<IActionResult> GetIconsSpriteSheet(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "icons/sheet/{format}")] HttpRequest req,
            ILogger log, string format)
        {
            var icon = await _iconsService.GetSpriteSheet(format);
            if (icon == null) return new StatusCodeResult(404);
            var mime = format switch
            {
                "json" => "application/json",
                "png" => "image/png",
                "webp" => "image/webp",
                _ => "application/octet-stream"
            };
            return new FileStreamResult(icon, mime);
        }

        [FunctionName("GetIcon")]
        public async Task<IActionResult> GetIcon(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "icons/{id:int}")] HttpRequest req,
            ILogger log, int id)
        {
            var icon = await _iconsService.GetIcon(id);
            if (icon == null) return new NotFoundResult();
            return new OkObjectResult(icon);
        }
    }
}
