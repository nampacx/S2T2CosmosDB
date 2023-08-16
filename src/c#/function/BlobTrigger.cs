using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace S2T2CosmosDB.Function
{
    public class BlobTrigger
    {
        private readonly IMongoCollection<BsonDocument> collection;

        public BlobTrigger(IMongoCollection<BsonDocument> collection)
        {
            this.collection = collection;
        }

        [FunctionName("BlobTrigger")]
        public async Task Run([BlobTrigger("myblobcontainer/{name}", Connection = "AzureWebJobsStorage")] Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            if (!string.IsNullOrEmpty(name))
            {
                // Add a JSON document to the output container.
                await collection.InsertOneAsync(new BsonDocument
                {
                    { "name", name },
                    { "size", myBlob.Length }
                });
            }
        }
    }
}
