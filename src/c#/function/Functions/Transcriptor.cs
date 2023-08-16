using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.DataMovement;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using RestSharp;
using CloudBlobClient = Microsoft.Azure.Storage.Blob.CloudBlobClient;
using CloudBlobContainer = Microsoft.Azure.Storage.Blob.CloudBlobContainer;
using CloudBlockBlob = Microsoft.Azure.Storage.Blob.CloudBlockBlob;

namespace S2T2CosmosDB.Function.Functions
{
    public class Transcriptor
    {
        private static string[] audioFiles = new string[] { ".mp3", ".wav", ".wma", ".m4a" };
        private readonly SpeechOptions _speechOptions;
        private TranscriptOptions _transcriptOptions;
        private static string SpeechKeyHeaderName = "Ocp-Apim-Subscription-Key";

        public Transcriptor(IOptions<SpeechOptions> options, IOptions<TranscriptOptions> transactionOptions)
        {
            _speechOptions = options.Value;
            _transcriptOptions = transactionOptions.Value;
        }

        [FunctionName("Transcriptor")]
        public async Task Run([BlobTrigger("audio/{name}", Connection = "Audio:StorageAccountConnectionString")] Stream myBlob, string name, Uri uri, ILogger log)
        {
            log.LogInformation($"C# Blob trigger function Processed blob\n Name:{name} \n myBlob: {uri}");

            if (!audioFiles.Contains(Path.GetExtension(name)))
            {
                log.LogInformation($"None audio file triggerd this function.");
                return;
            }


            var transcriptionRequest = new TranscriptionRequest { contentUrls = new string[] { uri.ToString() } };

            var baseUri = $"https://{_speechOptions.Region}.api.cognitive.microsoft.com";
            var options = new RestClientOptions(baseUri);
            var client = new RestClient(options);

            var startTranscriptionRequest = new RestRequest("/speechtotext/v3.1/transcriptions", Method.Post);
            startTranscriptionRequest.AddHeader(SpeechKeyHeaderName, _speechOptions.Key);
            startTranscriptionRequest.AddHeader("Content-Type", "application/json");
            startTranscriptionRequest.AddStringBody(JsonConvert.SerializeObject(transcriptionRequest), DataFormat.Json);
            var response = await client.ExecuteAsync(startTranscriptionRequest);

            log.LogInformation($"Transcription started: {response.StatusCode}");

            var transcriptionResponse = JsonConvert.DeserializeObject<TranscriptResponse>(response.Content);
            var statusRequest = new RestRequest(transcriptionResponse.self.AbsolutePath);
            statusRequest.AddHeader(SpeechKeyHeaderName, _speechOptions.Key);

            do
            {
                await Task.Delay(1500);
                response = await client.ExecuteAsync(statusRequest);
                log.LogInformation($"Transcription started: {response.StatusCode}");
                transcriptionResponse = JsonConvert.DeserializeObject<TranscriptResponse>(response.Content);
            } while (transcriptionResponse.status != "Succeeded");

            var filesRequest = new RestRequest(transcriptionResponse.links.files.AbsolutePath);
            filesRequest.AddHeader(SpeechKeyHeaderName, _speechOptions.Key);
            response = await client.ExecuteAsync(filesRequest);
            log.LogInformation($"Transcription started: {response.StatusCode}");

            var fileResponse = JsonConvert.DeserializeObject<FilesResponse>(response.Content);
            var dateTimeString = DateTime.Now.ToString("yyyy_MM_dd_hh_mm_ss");
            foreach (var item in fileResponse.values)
            {
                var blobName = $"{name.Replace(".", "_")}_{item.kind}_${dateTimeString}.json";
                log.LogInformation($"File: {blobName}");
                await TransferUrlToAzureBlob(item.links.contentUrl, blobName);
            }            
        }

        public async Task TransferUrlToAzureBlob(Uri sourceUri, string targetBlobName)
        {
            var account = CloudStorageAccount.Parse(_transcriptOptions.StorageAccountConnectionString);
            var blobClient = account.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(_transcriptOptions.ContainerName);
            await container.CreateIfNotExistsAsync();
            var blob = container.GetBlockBlobReference(targetBlobName);
            await TransferManager.CopyAsync(sourceUri, blob, true);
        }
    }
    public class TranscriptResponse
    {
        public Uri self { get; set; }

        public string status { get; set; }

        public Links links { get; set; }
    }

    public class Links
    {
        public Uri files { get; set; }
        public Uri contentUrl { get; set; }
    }

    public class FilesResponse
    {
        public Value[] values { get; set; }

    }

    public class Value
    {
        public string name { get; set; }
        public string kind { get; set; }
        public Links links { get; set; }
    }

    public class LanguageIdentification
    {
        public string[] candidateLocales { get; set; } = new string[] { "en-US", "de-DE" };
    }

    public class Properties
    {
        public bool wordLevelTimestampsEnabled { get; set; } = true;
        public LanguageIdentification languageIdentification { get; set; } = new LanguageIdentification();
    }

    public class TranscriptionRequest
    {
        public string[] contentUrls { get; set; }
        public string locale { get; set; } = "en-US";
        public string displayName { get; set; } = "Transcript";
        public object model { get; set; }
        public Properties properties { get; set; } = new Properties();
    }
}
