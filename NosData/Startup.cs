using System.Text;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using NosData.Services;

[assembly: FunctionsStartup(typeof(NosData.Startup))]
namespace NosData
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddLogging();
            builder.Services.AddSingleton<BlobsService>();
            builder.Services.AddSingleton<NosFileService>();
            builder.Services.AddSingleton<ExecutableVersionService>();
            builder.Services.AddSingleton<IconsService>();
            builder.Services.AddSingleton<DataService>();
            builder.Services.AddSingleton<TranslationsService>();
            builder.Services.AddSingleton<RefreshService>();

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }
    }
}