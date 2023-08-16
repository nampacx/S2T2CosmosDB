using System;
using System.IO;
using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace S2T2CosmosDB.Function.Functions
{
    public class Feeder
    {
        private readonly IMongoCollection<BsonDocument> collection;

        public Feeder(IMongoCollection<BsonDocument> collection)
        {
            this.collection = collection;
        }

        [FunctionName("Feeder")]
        public async Task Run([BlobTrigger("transcript/{name}.json", Connection = "Transcript:StorageAccountConnectionString")] Stream myBlob, string name, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");

            var json = StreamToString(myBlob);

            var document = JsonConvert.DeserializeObject<Transcript>(json);

            await collection.InsertOneAsync(document.ToBsonDocument());
        }

        public static string StreamToString(Stream stream)
        {
            stream.Position = 0;
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }


    }

    public class Transcript
    {
        public string source { get; set; }
        public DateTime timestamp { get; set; }
        public int durationInTicks { get; set; }
        public string duration { get; set; }
        public List<CombinedRecognizedPhrase> combinedRecognizedPhrases { get; set; }

    }

    public class CombinedRecognizedPhrase
    {
        public int channel { get; set; }
        public string lexical { get; set; }
        public string itn { get; set; }
        public string maskedITN { get; set; }
        public string display { get; set; }
    }
}
