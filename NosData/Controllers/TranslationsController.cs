using System;
using System.IO;
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
    public class TranslationsController
    {
        private readonly TranslationsService _translationsService;

        public TranslationsController(TranslationsService translationsService)
        {
            _translationsService = translationsService;
        }

        [FunctionName("GetTranslations")]
        [FunctionResponseCache(60 * 60, ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetTranslations(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "translations/{language}/{type}")] HttpRequest req,
            ILogger log, string language, string type)
        {
            if (!TranslationsService.Languages.Contains(language))
            {
                return new NotFoundResult();
            }

            if (!TranslationsService.LanguageFiles.ContainsKey(type))
            {
                return new NotFoundResult();
            }

            var data = await _translationsService.GetTranslations(language, type);

            if (data == null) return new StatusCodeResult(503);
            return new OkObjectResult(data);
        }

        [FunctionName("GetRawTranslations")]
        [FunctionResponseCache(60 * 60, ResponseCacheLocation.Any)]
        public async Task<IActionResult> GetRawTranslations(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "translations/{language}/{type}/raw")] HttpRequest req,
            ILogger log, string language, string type)
        {
            if (!TranslationsService.Languages.Contains(type))
            {
                return new NotFoundResult();
            }

            if (!TranslationsService.LanguageFiles.ContainsKey(type))
            {
                return new NotFoundResult();
            }

            var data = await _translationsService.GetRawTranslations(language, type);

            if (data == null) return new StatusCodeResult(503);
            return new FileStreamResult(data, "text/plain");
        }
    }
}
