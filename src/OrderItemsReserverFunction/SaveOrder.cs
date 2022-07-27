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
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;

namespace OrderItemsReserverFunction
{
    public class SaveOrder
    {
       
        [FunctionName("SaveOrder")]
        public async Task<string> Run(
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
