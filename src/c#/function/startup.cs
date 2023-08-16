using System;
using System.Reflection;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using S2T2CosmosDB.Function.Functions;

[assembly: FunctionsStartup(typeof(S2T2CosmosDB.Function.Startup))]

namespace S2T2CosmosDB.Function
{
    public class Startup : FunctionsStartup
    {
        private IConfigurationRoot _config;

        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddOptions<SpeechOptions>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection("Speech").Bind(settings);
                });
            
            builder.Services.AddOptions<AudioOptions>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection("Audio").Bind(settings);
                });

            builder.Services.AddOptions<TranscriptOptions>()
                .Configure<IConfiguration>((settings, configuration) =>
                {
                    configuration.GetSection("Transcript").Bind(settings);
                });

            builder.Services.AddSingleton(p =>
            {
                var connectionString = _config.GetValue<string>("CosmosDbConnectionString");
                var database = _config.GetValue<string>("MongoDBDatabase");
                var collection = _config.GetValue<string>("MongoDBCollection");
                return new MongoClient(connectionString).GetDatabase(database).GetCollection<BsonDocument>(collection);
            });
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();
            var env = context.EnvironmentName;

            // add azure configuration
            if (env.Equals("Development", StringComparison.OrdinalIgnoreCase))
            {
                builder.ConfigurationBuilder.AddUserSecrets(Assembly.GetExecutingAssembly(), true, true);
            }
             _config = builder.ConfigurationBuilder.Build();
        }
    }
}