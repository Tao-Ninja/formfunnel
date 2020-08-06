using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using Microsoft.AspNetCore.Http.Extensions;
using System.Collections.Generic;

namespace Company.Function
{
    public static class isHuman
    {
        [FunctionName("isHuman")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                string token = data?.token;

                if (string.IsNullOrWhiteSpace(token))
                {
                    return new BadRequestObjectResult("Please pass token in body");
                }
                // validate recaptcha token
                using (var client = new HttpClient())
                {
                    var query = new QueryBuilder();
                    query.Add("secret", Environment.GetEnvironmentVariable("RecaptchaSecretKey"));
                    query.Add("response", token);
                    query.Add("remoteIp", req.HttpContext.Connection.RemoteIpAddress.ToString());
                    var uri = new UriBuilder("https://www.google.com/recaptcha/api/siteverify");
                    uri.Query = query.ToString();


                    var request = new HttpRequestMessage(HttpMethod.Post, uri.ToString());
                    var response = await client.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                    {
                        return new BadRequestObjectResult("Recaptcha rejected our request");  // recaptcha rejected our request
                    }



                    var responseString = await response.Content.ReadAsStringAsync();
                    Dictionary<string,dynamic> responseData = JsonConvert.DeserializeObject<Dictionary<string,dynamic>>(responseString);
                    log.LogInformation("Response Data." + JsonConvert.SerializeObject(responseData));

                    if (responseData.ContainsKey("success") && responseData.ContainsKey("score") && "true".Equals(responseData["success"].ToString())  && (Convert.ToDouble(responseData["score"]) >= 0.5))
                    {
                        return new OkObjectResult("Human");
                    }
                    else if (responseData.ContainsKey("success") && "true".Equals(responseData["success"].ToString()))
                    {
                        return new BadRequestObjectResult("Not Human");
                    }
                    return new BadRequestObjectResult("Error Response");
            }
        }
        catch(Exception ex)
        {
            return new BadRequestObjectResult("Error Response: "+ ex.ToString() );
        }
    }
}
}
