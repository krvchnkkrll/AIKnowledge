using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenSearch.Client;
using Opensearch.Options;

namespace Opensearch;

public static class DependencyInjection
{
    public static IHostApplicationBuilder AddOpensearch(this IHostApplicationBuilder builder)
    {
        builder.Services.AddOptions<OpensearchOptions>().BindConfiguration("Opensearch");
        
        builder.Services.AddSingleton<IConnectionSettingsValues>(static serviceProvider =>
        {
            var openSearchOptions = serviceProvider.GetRequiredService<IOptions<OpensearchOptions>>().Value;
            
            var connectionSettings = new ConnectionSettings(new Uri(openSearchOptions.Url))
                .BasicAuthentication(openSearchOptions.Username, openSearchOptions.Password)
                .DefaultIndex(openSearchOptions.Index);
            
            return connectionSettings;
        });
        
        builder.Services.AddSingleton<IOpenSearchClient>(static serviceProvider =>
        {
            var connectionSettings = serviceProvider.GetRequiredService<IConnectionSettingsValues>();
            var client = new OpenSearchClient(connectionSettings);
            return client;
        });
        
        return builder;
    }
}