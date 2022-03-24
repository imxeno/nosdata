using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NosData.DTOs;
using NosData.Services;
using NosData.Utils;

namespace NosData
{
    public class ExecutableVersionController
    {
        private readonly ExecutableVersionService _executableVersionService;

        public ExecutableVersionController(ExecutableVersionService executableVersionService)
        {
            _executableVersionService = executableVersionService;
        }

        [FunctionName("GetExecutableVersion")]
        public async Task<IActionResult> GetExecutableVersion(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "executableVersion")] HttpRequest req,
            ILogger log)
        {
            var version = await _executableVersionService.GetExecutableVersion();
            if (version == null) return new StatusCodeResult(503);
            return new OkObjectResult(version);
        }
    }
}
