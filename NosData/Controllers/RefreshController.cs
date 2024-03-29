using System;
using System.IO;
using System.Text;
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
        private readonly RefreshService _refreshService;

        public RefreshController(RefreshService refreshService)
        {
            _refreshService = refreshService;
        }

        [FunctionName("AdminRefresh")]
        public async Task<IActionResult> AdminRefresh(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "refresh")]
            HttpRequest req,
            ILogger log)
        {
            var refreshed = await _refreshService.RefreshAll();
            if (refreshed)
            {
                return new OkResult();
            }

            return new ConflictResult();
        }

        [FunctionName("AdminForceRefresh")]
        public async Task<IActionResult> AdminForceRefresh(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "refresh/force")]
            HttpRequest req,
            ILogger log)
        {
            var refreshed = await _refreshService.RefreshAll(true);
            if (refreshed)
            {
                return new OkResult();
            }

            return new ConflictResult();
        }

        [FunctionName("AutoRefresh")]
        public async Task AutoRefreshAll([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer, ILogger log)
        {
            await _refreshService.RefreshAll();
        }
    }
}
