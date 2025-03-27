using Entitys;
using Markdig;
using NLog;
using RestSharp;
using System;
using System.Threading.Tasks;

namespace Work
{
    /// <summary>
    /// 火山引擎API调用
    /// </summary>
    public class Volcengine
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static async Task<ReturnValue<string>> ChatCompletions(string apikey, string prompt)
        {
            ReturnValue<string> rv = new ReturnValue<string>();
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
                try
                {
                    // 解析JSON响应
                    var jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response.Content);

                    // 提取content字段
                    string content = jsonResponse["choices"][0]["message"]["content"];

                    rv.True(content);
                }
                catch (Exception ex) 
                {
                    logger.Error(ex);
                    logger.Info(response.Content);
                    rv.False(response.Content);
                }
            }
            else
            {
                string msg = response.StatusCode + "|" + response.ErrorMessage + response.Content;
                rv.False(msg);
                logger.Info(msg);   
            }
            return rv;
        }


        public static string MD2Html(string content)
        {
            // 去除转义字符
            content = System.Text.RegularExpressions.Regex.Unescape(content);

            // 将Markdown转换为HTML
            string htmlContent = Markdown.ToHtml(content);

            return htmlContent;
        }
    }
}
