using System.IO.Compression;
using Microsoft.AspNetCore.ResponseCompression;

namespace Web;

internal static class DependencyInjection
{
    public static IHostApplicationBuilder AddWeb(this IHostApplicationBuilder builder)
    {
        builder.Services.AddControllers();

        builder.Services.AddEndpointsApiExplorer();
        
        builder.Services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });

        builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });

        builder.Services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });
        
        builder.Services.AddHealthChecks();

        return builder;
    }
}