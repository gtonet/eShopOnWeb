﻿using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Text;

namespace OrderItemsReserverFunction
{
    public class SaveOrder
    {

        [FunctionName("SaveOrder")]
        [FixedDelayRetry(3, "00:00:10")]
        public async Task<string> Run(
            ExecutionContext context,
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
             [Blob("orders", FileAccess.ReadWrite, Connection = "StorageConnectionString")] Azure.Storage.Blobs.BlobContainerClient blobContainer,
            ILogger log)
        {
            log.LogInformation("Received order info.");
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

                var currentRetryCount = context.RetryContext.RetryCount;
                if (currentRetryCount >= context.RetryContext.MaxRetryCount)
                {
                    var config = new ConfigurationBuilder()
                        .SetBasePath(context.FunctionAppDirectory)
                        .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                        .AddEnvironmentVariables()
                        .Build();

                    var logicAppUrl = config["LOGIC_APP_URL"];
                    await SendEmail(logicAppUrl);
                }
                else
                {
                    throw exp;
                }
            }

            return responseMessage;
        }

        private async Task SendEmail(string logicAppUrl)
        {

            var client = new HttpClient();
            var jsonData = JsonConvert.SerializeObject(new
            {
                email = "<name>@outlook.com",
                due = System.DateTime.Now,
                task = "Save order data failed."
            });

            HttpResponseMessage result = await client.PostAsync(
                logicAppUrl,
                new StringContent(jsonData, Encoding.UTF8, "application/json"));

            var statusCode = result.StatusCode.ToString();
        }

        [FunctionName("SaveOrder01")]
        public async Task<string> Run01(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
             [CosmosDB("eShop1",
                      "OrderInfo",
                      ConnectionStringSetting = "CosmosConnectionStringSetting",
                      CreateIfNotExists = true)] IAsyncCollector<dynamic> items,
            ILogger log)
        {
            log.LogInformation("Received an order.");
            string responseMessage = String.Empty;
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            try
            {
                var newId = Guid.NewGuid();

                var order = JsonConvert.DeserializeObject<Order>(body);
                var item = new
                {
                    id = newId,
                    orderInfo = body
                };

                await items.AddAsync(order); // 

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