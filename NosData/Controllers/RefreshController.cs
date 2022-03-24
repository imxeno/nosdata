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
    public class RefreshController
    {
        private readonly DataService _dataService;
        private readonly ExecutableVersionService _executableVersionService;
        private readonly IconsService _iconsService;

        public RefreshController(DataService dataService, ExecutableVersionService executableVersionService,
            IconsService iconsService)
        {
            _dataService = dataService;
            _executableVersionService = executableVersionService;
            _iconsService = iconsService;
        }

        [FunctionName("AdminRefreshIcons")]
        public async Task<IActionResult> AdminRefreshIcons(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "refresh/icons")] HttpRequest req,
            ILogger log)
        {
            await _iconsService.RefreshIcons(null, log);
            return new OkResult();
        }

        [FunctionName("AdminRefreshData")]
        public async Task<IActionResult> AdminRefreshData(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "refresh/data")] HttpRequest req,
            ILogger log)
        {
            await _dataService.RefreshData(null, log);
            return new OkResult();
        }

        [FunctionName("AdminRefreshExecutableVersion")]
        public async Task<IActionResult> AdminRefreshExecutableVersion(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "refresh/executableVersion")] HttpRequest req,
            ILogger log)
        {
            await _executableVersionService.RefreshExecutableVersion(null, log);
            return new OkResult();
        }
    }
}
