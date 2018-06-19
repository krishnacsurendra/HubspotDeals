using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Collections.Generic;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            //get all the pipeline stages
            var pipelineUrl = "https://api.hubapi.com/deals/v1/pipelines/default?hapikey=ea033928-93c5-4afd-a99e-05e446230128";
            string json = GetJsonResult(pipelineUrl);
            dynamic deals = JsonConvert.DeserializeObject(json);
            List<string> stages = new List<string>();

            foreach (var stage in deals.stages)
            {
                stages.Add(stage.stageId.ToString());
            }

            //get deals in bulk
            string recentDealsUrl = @"https://api.hubapi.com/deals/v1/deal/paged?limit=250&properties=dealstage&includeAssociations=true&hapikey=ea033928-93c5-4afd-a99e-05e446230128&";
            var temp = recentDealsUrl;
            do
            {
                json = GetJsonResult(temp);
                deals = JsonConvert.DeserializeObject(json);

                foreach (var deal in deals.deals)
                {
                    //get the deal stageid from deal properties
                    var dealstage = deal.properties["dealstage"].value.ToString();
                    if (!stages.Contains(dealstage))
                        continue;
                    var companyId = deal.associations.associatedCompanyIds.Count > 0 ? deal.associations.associatedCompanyIds[0] : 0;
                    if (companyId == 0)
                        continue;
                    var dealId = deal.dealId;
                    string companyUrl = "https://api.hubapi.com/companies/v2/companies/" + companyId + "?hapikey=ea033928-93c5-4afd-a99e-05e446230128";
                    json = GetJsonResult(companyUrl);
                    dynamic company = JsonConvert.DeserializeObject(json);
                    var dealScore = company.properties.deal_score != null ? company.properties.deal_score.value.ToString() : 0;

                    var request = "https://api.hubapi.com/deals/v1/deal/" + dealId + "?hapikey=ea033928-93c5-4afd-a99e-05e446230128";

                    var postData = "{\"properties\": [{\"name\": \"deal_score\",\"value\": " + dealScore + "}]}";

                    UpdateProductAsync(request, postData).GetAwaiter().GetResult();
                }
                temp = recentDealsUrl + "offset=" + deals.offset;
            }
            while (deals.offset > 0);
        }

        static async Task UpdateProductAsync(string request, string postData)
        {
            using (var httpClient = new HttpClient())
            {
                var postContent = new StringContent(postData);
                postContent.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/json");
                var httpResponse = await httpClient.PutAsync(request, postContent);
            }
        }

        private static string GetJsonResult(string apiUrl)
        {
            //Send webRequest
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(apiUrl);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream responseStream = response.GetResponseStream();

            //Seting Up the Stream Reader
            StreamReader readerStream = new StreamReader(responseStream, System.Text.Encoding.GetEncoding("utf-8"));

            string json = readerStream.ReadToEnd();
            readerStream.Close();
            return json;
        }   

        private static string ReadStreamFromResponse(WebResponse response)
        {
            using (Stream responseStream = response.GetResponseStream())
            using (var sr = new StreamReader(responseStream))
            {
                //Need to return this response 
                string strContent = sr.ReadToEnd();
                return strContent;
            }
        }
    }
}