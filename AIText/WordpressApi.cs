using RestSharp;
using SqlSugar.Extensions;
using System;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace AIText
{
    public class WordpressApi
    {
        public static async Task<string> GetAccessToken(string site, string user, string pwd)
        {
            var options = new RestClientOptions(site);
            var client = new RestClient(options);
            var request = new RestRequest("/index.php/wp-json/api/v1/token", Method.Post);
            request.AddParameter("username", user);
            request.AddParameter("password", pwd);
            RestResponse response = await client.ExecuteAsync(request);
            if (response.IsSuccessStatusCode)
            {
                if (!string.IsNullOrWhiteSpace(response.Content) && (response.Content.StartsWith("{") || response.Content.StartsWith("[")))
                {
                    // 解析JSON响应
                    var jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response.Content);
                    // 提取jwt_token字段
                    string token = jsonResponse["jwt_token"];
                    //// 提取expires_in字段
                    //string expire = jsonResponse["expires_in"];
                    return token;
                }
                else
                {
                    return response.StatusCode + "|" + response.Content;
                }
            }
            else
            {
                Console.WriteLine(response.StatusCode);
                return response.StatusCode + "|" + response.ErrorMessage + response.Content;
            }
        }

        public static async Task<string> PostToCreate(string site, string accesskey, string title, string content) 
        {
            var options = new RestClientOptions(site);
            var client = new RestClient(options);
            var request = new RestRequest("/wp-json/wp/v2/posts", Method.Post);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Authorization", $"Bearer {accesskey}");
            var body = new 
            {
                title = title,
                content = content,
                status = "publish"
            };
            request.AddJsonBody(body);
            RestResponse response = await client.ExecuteAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return response.Content;
            }
            else
            {
                Console.WriteLine(response.StatusCode);
                return response.StatusCode.ObjToInt() + "|" + response.ErrorMessage + response.Content;
            }
        }
    }
}
