using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NosData.Utils;

namespace NosData
{
    public class Program
    {
        public static void Main()
        {
            var host = new HostBuilder()
                .ConfigureFunctionsWorkerDefaults()
                .ConfigureServices(s => { s.AddSingleton<AzureBlobCache, AzureBlobCache>(); })
                .Build();

            host.Run();
        }
    }
}