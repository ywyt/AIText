using Entitys;
using NLog;
using RestSharp;
using SqlSugar.Extensions;
using System;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace Work
{
    public class WordpressApi
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        public static async Task<ReturnValue<string>> GetAccessToken(string site, string user, string pwd)
        {
            ReturnValue<string> rv = new ReturnValue<string>();
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
                    try
                    {
                        // 解析JSON响应
                        var jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response.Content);
                        // 提取jwt_token字段
                        string token = jsonResponse["jwt_token"];
                        //// 提取expires_in字段
                        //string expire = jsonResponse["expires_in"];
                        rv.True(token);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                        logger.Info(response.Content);
                        rv.False("获取json结果出错");
                    }
                }
                else
                {
                    logger.Info(response.Content);
                    rv.False(response.StatusCode + "|" + response.Content);
                }
            }
            else
            {
                logger.Info(response.StatusCode + "|" + response.ErrorMessage + response.Content);
                rv.False(response.StatusCode + "|" + response.ErrorMessage + response.Content);
            }
            return rv;
        }

        public static async Task<ReturnValue<string>> PostToCreate(string site, string accesskey, string title, string content) 
        {
            ReturnValue<string> rv = new ReturnValue<string>();
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
                rv.True(response.Content);
            }
            else
            {
                string msg = response.StatusCode.ObjToInt() + "|" + response.ErrorMessage + response.Content;
                logger.Info(msg);
                rv.False(msg);
            }
            return rv;
        }

        public static async Task<ReturnValue<string>> UploadImage(string site, string accesskey, string path, string keyword)
        {
            ReturnValue<string> rv = new ReturnValue<string>();
            var options = new RestClientOptions(site);
            var client = new RestClient(options);
            var request = new RestRequest("/wp-json/wp/v2/media", Method.Post);
            request.AddHeader("Authorization", $"Bearer {accesskey}");
            request.AlwaysMultipartFormData = true;
            request.AddFile("file", path);

            RestResponse response = await client.ExecuteAsync(request);
            if (response.IsSuccessStatusCode)
            {
                if (!string.IsNullOrWhiteSpace(response.Content) && (response.Content.StartsWith("{") || response.Content.StartsWith("[")))
                {
                    try
                    {
                        // 解析JSON响应
                        var jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response.Content);
                        // 提取链接
                        string source_url = jsonResponse["source_url"];
                        rv.True(source_url);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                        logger.Info(response.Content);
                        rv.False("获取json结果出错");
                    }
                }
                else
                {
                    logger.Info(response.Content);
                    rv.False(response.StatusCode + "|" + response.Content);
                }
            }
            else
            {
                string msg = response.StatusCode.ObjToInt() + "|" + response.ErrorMessage + response.Content;
                logger.Info(msg);
                rv.False(msg);
            }
            return rv;
        }

        public static async Task<ReturnValue<string>> UploadImage(string site, string accesskey, byte[] imageBytes, string keyword, string filename = null)
        {
            ReturnValue<string> rv = new ReturnValue<string>();
            var options = new RestClientOptions(site);
            var client = new RestClient(options);
            var request = new RestRequest("/wp-json/wp/v2/media", Method.Post);
            request.AddHeader("Authorization", $"Bearer {accesskey}");
            request.AlwaysMultipartFormData = true;
            request.AddFile("file", bytes: imageBytes, filename ?? (keyword + ".jpg"), contentType: ContentType.Binary);

            RestResponse response = await client.ExecuteAsync(request);
            if (response.IsSuccessStatusCode)
            {
                if (!string.IsNullOrWhiteSpace(response.Content) && (response.Content.StartsWith("{") || response.Content.StartsWith("[")))
                {
                    try
                    {
                        // 解析JSON响应
                        var jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response.Content);
                        // 提取链接
                        string source_url = jsonResponse["source_url"];
                        rv.True(source_url);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex);
                        logger.Info(response.Content);
                        rv.False("获取json结果出错");
                    }
                }
                else
                {
                    logger.Info(response.Content);
                    rv.False(response.StatusCode + "|" + response.Content);
                }
            }
            else
            {
                string msg = response.ErrorMessage + response.Content;
                logger.Info(msg);
                rv.False(msg);
            }
            return rv;
        }
    }
}
