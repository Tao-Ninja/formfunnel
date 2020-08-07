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
        [FunctionName("isHumanFunctionName")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "isHuman")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            Dictionary<string,dynamic> responseDict = new Dictionary<string, dynamic>();
            responseDict["success"] = false;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                string token = data?.token;

                if (string.IsNullOrWhiteSpace(token))
                {
                    responseDict["message"] = "Please pass token in body";
                    return new BadRequestObjectResult(responseDict);
                }
                // validate recaptcha token
                using (var client = new HttpClient())
                {
                    var query = new QueryBuilder();
                    // Secret Key in global environment with name "RecaptchaSecretKey"
                    query.Add("secret", Environment.GetEnvironmentVariable("RecaptchaSecretKey"));
                    query.Add("response", token);
                    query.Add("remoteIp", req.HttpContext.Connection.RemoteIpAddress.ToString());
                    var uri = new UriBuilder("https://www.google.com/recaptcha/api/siteverify");
                    uri.Query = query.ToString();

                    var request = new HttpRequestMessage(HttpMethod.Post, uri.ToString());
                    var response = await client.SendAsync(request);
                    if (!response.IsSuccessStatusCode)
                    {
                        responseDict["message"] = "Recaptcha rejected our request";
                        return new BadRequestObjectResult(responseDict);
                    }



                    var responseString = await response.Content.ReadAsStringAsync();
                    Dictionary<string,dynamic> responseData = JsonConvert.DeserializeObject<Dictionary<string,dynamic>>(responseString);
                    
                    // making dictionary for post to successTrigger and errorTrigger
                    var innerClient = new HttpClient();
                    Dictionary<string,dynamic> requestbody = new Dictionary<string, dynamic>();
                    if (responseData.ContainsKey("score"))
                    {
                        requestbody["token"] = token;
                        requestbody["score"] = Convert.ToDouble(responseData["score"]);
                    }
                    
                    
                    // If success 
                    if (responseData.ContainsKey("success") && responseData.ContainsKey("score") && "true".Equals(responseData["success"].ToString())  && (Convert.ToDouble(responseData["score"]) >= 0.5))
                    {
                        // redirect uri in global environment with name "successTrigger"
                        try
                        {
                            HttpResponseMessage responseMessage = await innerClient.PostAsJsonAsync(Environment.GetEnvironmentVariable("successTrigger"),requestbody);
                        }
                        catch(Exception){}



                        responseDict["message"] = "Human";
                        responseDict["success"] = true;
                        responseDict["data"] = responseData;
                        return new OkObjectResult(responseDict);
                    }
                    // if not success
                    else if (responseData.ContainsKey("success") && "true".Equals(responseData["success"].ToString()))
                    {
                        try
                        {
                            HttpResponseMessage responseMessage = await innerClient.PostAsJsonAsync(Environment.GetEnvironmentVariable("errorTrigger"),requestbody);
                        }
                        catch(Exception){}



                        responseDict["message"] = "Not Human";
                        responseDict["data"] = responseData;
                        return new BadRequestObjectResult(responseDict);
                    }
                    responseDict["message"] = "Error Response";
                    return new BadRequestObjectResult(responseDict);
            }
        }
        catch(Exception ex)
        {
            responseDict["message"] = "Error Response: "+ ex.ToString();
            return new BadRequestObjectResult(responseDict);
        }
    }
}
}
