using Microsoft.Extensions.Logging;
using NosData.NosPack;
using NosData.Utils;
using System.Linq;
using System.Text;

namespace NosData.Services
{
    public class NosFileService
    {
        private readonly ILogger<NosFileService> _logger;

        public NosFileService(ILogger<NosFileService> logger)
        {
            _logger = logger;
        }

        public NTDataContainer FetchDataContainer(string name)
        {
            var data = FetchNosTaleUpdateFile(name);
            return NTDataContainer.Load(data);
        }

        public NTStringContainer FetchStringContainer(string name)
        {
            var data = FetchNosTaleUpdateFile(name);
            return NTStringContainer.Load(data);
        }

        public byte[] FetchNosTaleUpdateFile(string name)
        {
            var lowerCaseName = name.ToLower();
            _logger.LogInformation("Fetching data for " + name);
            var spark = SparkNosTaleDataSource.Latest();
            var data = spark.FileEntries().Single(e => e.Key.ToLower().Contains(lowerCaseName)).Value.Download();

            return data;
        }

        public byte[] FetchNosTaleBinary(string name)
        {
            var lowerCaseName = name.ToLower();
            _logger.LogInformation("Fetching data for " + name);
            var spark = SparkNosTaleDataSource.Latest();
            var data = spark.FileEntries().Single(e => e.Key.ToLower().Contains(lowerCaseName)).Value.Download();

            return data;
        }
    }
}
