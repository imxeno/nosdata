using System.Collections.Generic;
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
    public class NosCDN
    {
        private readonly AzureBlobCache _blobCache;
        private readonly ILogger<NosCDN> _logger;

        private readonly Dictionary<string, string> _genericDatFiles = new()
        {
            {"bcards", "BCard.dat"},
            {"cards", "Card.dat"},
            {"items", "Item.dat"},
            {"monsters", "monster.dat"},
            {"skills", "Skill.dat"}, 
            {"quests", "quest.dat"},
            {"questprizes", "qstprize.dat"},
            {"teams", "team.dat"},
            {"fishes", "fish.dat"}
        };

        private readonly Dictionary<string, string> _rawOnlyDatFiles = new()
        {
            {"npctalks", "npctalk.dat"},
            {"tutorial", "tutorial.dat"},
            {"shoptypes", "shoptype.dat"},
            {"mapids", "MapIDData.dat"},
            {"mappoints", "MapPointData.dat"},
            {"questnpcs", "qstnpc.dat"}
        };

        public NosCDN(ILogger<NosCDN> logger, AzureBlobCache blobCache)
        {
            _blobCache = blobCache;
            _logger = logger;
        }

        [Function("Data/{type}")]
        public HttpResponseData Get([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
            FunctionContext executionContext, string type)
        {
            var lowerCaseType = type.ToLower();

            if (!_genericDatFiles.ContainsKey(lowerCaseType))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

            var datFile = FetchDatFile(_genericDatFiles[lowerCaseType]);

            if (datFile == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            response.WriteString(NosTaleDatToJsonConverter.Convert(datFile).ToString());

            return response;
        }

        [Function("Data/{type}/Raw")]
        public HttpResponseData GetRaw([HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpRequestData req,
            FunctionContext executionContext, string type)
        {
            var lowerCaseType = type.ToLower();
            string datFileName;

            if (_genericDatFiles.ContainsKey(lowerCaseType))
            {
                datFileName = _genericDatFiles[lowerCaseType];
            }
            else if (!_rawOnlyDatFiles.ContainsKey(lowerCaseType))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }
            else
            {
                datFileName = _rawOnlyDatFiles[lowerCaseType];
            }

            var datFile = FetchDatFile(datFileName);

            if (datFile == null)
            {
                return req.CreateResponse(HttpStatusCode.NotFound);
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            response.WriteString(datFile);

            return response;
        }

        private string FetchDatFile(string name)
        {

            var itemDatBytes = _blobCache.Load(name);
            if (itemDatBytes == null)
            {
                _logger.LogInformation("Serving freshly fetched data");
                var spark = SparkNosTaleDataSource.Latest();
                var nosGtdDataBytes = spark.FileEntries().Single(e => e.Key.ToLower().Contains("nsgtddata")).Value.Download();
                var nosGtdData = NTStringContainer.Load(nosGtdDataBytes);

                if (!nosGtdData.Entries.TryGetValue(name, out var datFile))
                {
                    return null;
                }

                itemDatBytes = datFile.Content;
                _blobCache.Save(name, itemDatBytes);
            }
            else
            {
                _logger.LogInformation("Serving data from Azure Blob Cache");
            }

            return System.Text.Encoding.ASCII.GetString(itemDatBytes);
        }
    }
}
