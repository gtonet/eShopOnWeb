using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;

namespace OrderItemsReserverFunction
{
    public class OrderItemsReserver
    {
        [FunctionName("OrderItemsReserver")]
        public async Task<string> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
             [Blob("basket", FileAccess.ReadWrite, Connection = "StorageConnectionString")] Azure.Storage.Blobs.BlobContainerClient blobContainer,
            ILogger log)
        {
            log.LogInformation("Received an order.");
            string responseMessage = String.Empty;
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            try
            {
                var newId = Guid.NewGuid();
                var blobName = $"{newId}.json";

                await blobContainer.CreateIfNotExistsAsync();
                var cloudBlockBlob = blobContainer.GetBlobClient(blobName);
                BinaryData data = new BinaryData(body);
                await cloudBlockBlob.UploadAsync(data);

                responseMessage = $"The order {newId} is upload successfully.";
            }
            catch (Exception exp)
            {
                responseMessage = $"Exception: {exp.Message}";
            }

            return responseMessage;
        }
    }
}
