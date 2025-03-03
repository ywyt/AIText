using Markdig;
using Microsoft.AspNetCore.Http;
using RestSharp;
using SqlSugar;
using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace AIText
{
    /// <summary>
    /// 火山引擎API调用
    /// </summary>
    public class Volcengine
    {
        public static async Task<string> ChatCompletions(string apikey, string prompt)
        {
            var options = new RestClientOptions("https://ark.cn-beijing.volces.com");
            var client = new RestClient(options);
            var request = new RestRequest("/api/v3/chat/completions", Method.Post);
            request.AddHeader("Content-Type", "application/json");
            request.AddHeader("Authorization", $"Bearer {apikey}");
            var sendBody = new
            {
                model = "deepseek-v3-241226",
                messages = new[]
                {
                    new { role = "system", content = "You are a helpful assistant." },
                    new { role = "user", content = prompt }
                }
            };
            //string body = Newtonsoft.Json.JsonConvert.SerializeObject(sendBody);
            //request.AddBody(body);
            request.AddJsonBody(sendBody);
            RestResponse response = await client.ExecuteAsync(request);
            if (response.IsSuccessStatusCode)
            {
                // 解析JSON响应
                var jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response.Content);

                // 提取content字段
                string content = jsonResponse["choices"][0]["message"]["content"];

                // 去除转义字符
                content = System.Text.RegularExpressions.Regex.Unescape(content);

                // 将Markdown转换为HTML
                string htmlContent = Markdown.ToHtml(content);

                return htmlContent;
            }
            else
            {
                Console.WriteLine(response.StatusCode);
                return response.StatusCode + "|" + response.ErrorMessage + response.Content;
            }
        }
    }
}
