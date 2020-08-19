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
        
        [FunctionName("form")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "form")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            Dictionary<string,dynamic> responseDict = new Dictionary<string, dynamic>();
            responseDict["success"] = false;
            try
            {
                // string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                // dynamic data = JsonConvert.DeserializeObject(requestBody);
                // string token = data?.token;
                IFormCollection formCollection = req.Form;
                string token = formCollection["token"];
                ICollection<string> keys= formCollection.Keys;
                var urlEncodedDctionaryData = new Dictionary<string, string>();
                foreach(string obj in keys)
                {
                    urlEncodedDctionaryData.Add(obj,Convert.ToString(formCollection[obj]));
                }


                if (string.IsNullOrWhiteSpace(token))
                {
                    responseDict["message"] = "Please pass token in body";
                    log.LogInformation("LogInfo: Please pass token in body");
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
                        log.LogInformation("LogInfo: Recaptcha rejected our request");
                        return new BadRequestObjectResult(responseDict);
                    }



                    var responseString = await response.Content.ReadAsStringAsync();
                    Dictionary<string,dynamic> responseData = JsonConvert.DeserializeObject<Dictionary<string,dynamic>>(responseString);
                    
                    // making dictionary for post to successTrigger and errorTrigger
                    var innerClient = new HttpClient();
                    if (responseData.ContainsKey("score"))
                    {
                        urlEncodedDctionaryData.Add("score",Convert.ToString(Convert.ToInt16(responseData["score"])/10));
                    }
                    
                    
                    // If success 
                    if (responseData.ContainsKey("success") && responseData.ContainsKey("score")  && responseData["success"] &&(responseData["score"] >= (Convert.ToInt16(Environment.GetEnvironmentVariable("captchaScore")))/10))
                    {
                        // redirect uri in global environment with name "successTrigger"
                        try
                        {
                            string triggerName = "successTrigger";
                            if (urlEncodedDctionaryData.ContainsKey("formType"))
                            {
                                triggerName = urlEncodedDctionaryData["formType"].ToLower() +  "Trigger";
                            }

                            HttpResponseMessage responseMessage = await innerClient.PostAsync(Environment.GetEnvironmentVariable(triggerName), new FormUrlEncodedContent(urlEncodedDctionaryData));
                            if (responseMessage.IsSuccessStatusCode)
                            {
                                log.LogInformation("LogInfo: successTrigger hit successfully with params"  + urlEncodedDctionaryData.ToString());
                            }
                            else
                            {
                            log.LogInformation("LogInfo: successTrigger fails with params" + responseMessage.RequestMessage.ToString() );
                            }
                        }
                        catch(Exception){
                            log.LogInformation("LogInfo: successTrigger fails" );
                        }
                        responseDict["message"] = "Human";
                        responseDict["success"] = true;
                        responseDict["data"] = responseData;
                        return new OkObjectResult(responseDict);
                    }
                    // if not success
                    else if (responseData.ContainsKey("success") && responseData["success"])
                    {
                        try
                        {
                            HttpResponseMessage responseMessage = await innerClient.PostAsJsonAsync(Environment.GetEnvironmentVariable("errorTrigger"),new FormUrlEncodedContent(urlEncodedDctionaryData));
                            if (responseMessage.IsSuccessStatusCode)
                            {
                            log.LogInformation("LogInfo: errorTrigger hit successfully with params"  + urlEncodedDctionaryData.ToString());

                            }
                            else
                            {
                            log.LogInformation("LogInfo: errorTrigger fails with params" + responseMessage.RequestMessage.ToString() );

                            }
                        }
                        catch(Exception){
                            log.LogInformation("LogInfo: errorTrigger fails" );
                        }
                        responseDict["message"] = "Not Human";
                        responseDict["data"] = responseData;
                        return new BadRequestObjectResult(responseDict);
                    }
                    responseDict["message"] = "Error Response";
                    responseDict["data"] = responseData;
                    log.LogInformation("LogInfo: Error Response");
                    return new BadRequestObjectResult(responseDict);
            }
        }
        catch(Exception ex)
        {
            responseDict["message"] = "Error Response: "+ ex.ToString();
            log.LogInformation("LogInfo: Error Response: "+ ex.ToString());
            return new BadRequestObjectResult(responseDict);
        }
    }
}
}
