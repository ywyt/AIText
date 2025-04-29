using Azure;
using Entitys;
using NLog;
using RestSharp;
using SqlSugar.Extensions;
using System;
using System.Drawing.Printing;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Work
{
    public class WordpressApi
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 获取JWT
        /// </summary>
        /// <param name="site"></param>
        /// <param name="user"></param>
        /// <param name="pwd"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 获取JWT
        /// </summary>
        /// <param name="site"></param>
        /// <param name="user"></param>
        /// <param name="pwd"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<string>> ValidateToken(string site, string accesskey)
        {
            ReturnValue<string> rv = new ReturnValue<string>();
            var options = new RestClientOptions(site);
            var client = new RestClient(options);
            var request = new RestRequest("/index.php/wp-json/api/v1/token-validate", Method.Get);
            request.AddHeader("Authorization", $"Bearer {accesskey}");
            RestResponse response = await client.ExecuteAsync(request);
            if (response.IsSuccessStatusCode)
            {
                rv.True("验证通过");
            }
            else
            {
                logger.Info(response.StatusCode + "|" + response.ErrorMessage + response.Content);
                rv.False(response.StatusCode + "|" + response.ErrorMessage + response.Content);
            }
            return rv;
        }

        /// <summary>
        /// post创建文章
        /// </summary>
        /// <param name="site"></param>
        /// <param name="accesskey"></param>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="retry"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<string>> PostToCreate(string site, string accesskey, string title, string content, int retry = 0) 
        {
            ReturnValue<string> rv = new ReturnValue<string>();
            var options = new RestClientOptions(site);
            var client = new RestClient(options);
            string path = "/wp-json/wp/v2/posts";
            if (retry > 0)
                path = "/index.php/wp-json/wp/v2/posts";
            var request = new RestRequest(path, Method.Post);
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
                if (retry < 1)
                {
                    logger.Info($"{site}同步失败，再次尝试");
                    return await PostToCreate(site, accesskey, title, content, ++retry);
                }
                string msg = response.StatusCode.ObjToInt() + "|" + response.ErrorMessage + response.Content;
                logger.Info(msg);
                rv.False(msg);
            }
            return rv;
        }

        /// <summary>
        /// 上传本地图片，暂时无用
        /// </summary>
        /// <param name="site"></param>
        /// <param name="accesskey"></param>
        /// <param name="path"></param>
        /// <param name="keyword"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<string>> UploadImage(string site, string accesskey, string path, string keyword, int retry = 0)
        {
            ReturnValue<string> rv = new ReturnValue<string>();
            var options = new RestClientOptions(site);
            var client = new RestClient(options);
            string uri = "/wp-json/wp/v2/media";
            if (retry > 0)
                uri = "/index.php/wp-json/wp/v2/media";
            var request = new RestRequest(uri, Method.Post);
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

        /// <summary>
        /// 上传图片字节流
        /// </summary>
        /// <param name="site"></param>
        /// <param name="accesskey"></param>
        /// <param name="imageBytes"></param>
        /// <param name="keyword"></param>
        /// <param name="filename"></param>
        /// <param name="retry"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<string>> UploadImage(string site, string accesskey, byte[] imageBytes, string keyword, string filename = null, int retry = 0)
        {
            ReturnValue<string> rv = new ReturnValue<string>();
            var options = new RestClientOptions(site);
            var client = new RestClient(options);
            string uri = "/wp-json/wp/v2/media";
            if (retry > 0)
                uri = "/index.php/wp-json/wp/v2/media";
            var request = new RestRequest(uri, Method.Post);
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
                if (retry < 1)
                {
                    return await UploadImage(site, accesskey, imageBytes, keyword, filename, ++retry);
                }
                string msg = response.ErrorMessage + response.Content;
                logger.Info(msg);
                rv.False(msg);
            }
            return rv;
        }

        /// <summary>
        /// 获取woocommerce的商品
        /// </summary>
        /// <param name="site"></param>
        /// <param name="wcKey"></param>
        /// <param name="wcSecret"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<string>> WcProducts(string site, string wcKey, string wcSecret, int page = 1, int pageSize = 10)
        {
            ReturnValue<string> rv = new ReturnValue<string>();
            var options = new RestClientOptions(site);
            var client = new RestClient(options);
            var request = new RestRequest("/wp-json/wc/v3/products", Method.Get);
            request.AddQueryParameter("page", page);
            request.AddQueryParameter("per_page", pageSize);
            // 设置 Basic Auth
            var authBytes = Encoding.UTF8.GetBytes($"{wcKey}:{wcSecret}");
            var authBase64 = Convert.ToBase64String(authBytes);
            request.AddHeader("Authorization", $"Basic {authBase64}");
            RestResponse response = await client.ExecuteAsync(request);
            if (response.IsSuccessStatusCode)
            {
                if (!string.IsNullOrWhiteSpace(response.Content) && (response.Content.StartsWith("{") || response.Content.StartsWith("[")))
                {
                    rv.True(response.Content);
                    //try
                    //{
                    //    // 解析JSON响应
                    //    var jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response.Content);
                    //    // 提取jwt_token字段
                    //    string token = jsonResponse["jwt_token"];
                    //    //// 提取expires_in字段
                    //    //string expire = jsonResponse["expires_in"];
                    //    rv.True(token);
                    //}
                    //catch (Exception ex)
                    //{
                    //    logger.Error(ex);
                    //    logger.Info(response.Content);
                    //    rv.False("获取json结果出错");
                    //}
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

        /// <summary>
        /// 获取woocommerce的商品总数
        /// </summary>
        /// <param name="site"></param>
        /// <param name="wcKey"></param>
        /// <param name="wcSecret"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<int>> WcProductsTotal(string site, string wcKey, string wcSecret)
        {
            ReturnValue<int> rv = new ReturnValue<int>();
            var options = new RestClientOptions(site);
            var client = new RestClient(options);
            var request = new RestRequest("/wp-json/wc/v3/products", Method.Get);
            request.AddQueryParameter("page", 1);
            request.AddQueryParameter("per_page", 1);
            // 设置 Basic Auth
            var authBytes = Encoding.UTF8.GetBytes($"{wcKey}:{wcSecret}");
            var authBase64 = Convert.ToBase64String(authBytes);
            request.AddHeader("Authorization", $"Basic {authBase64}");
            RestResponse response = await client.ExecuteAsync(request);
            if (response.IsSuccessStatusCode)
            {
                if (!string.IsNullOrWhiteSpace(response.Content) && (response.Content.StartsWith("{") || response.Content.StartsWith("[")))
                {
                    foreach (var header in response.Headers)
                    {
                        if (header.Name.Equals("X-WP-Total", StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(header.Value.ToString(), out int total))
                            {
                                logger.Info($"{site}获取商品总数{total}");
                                rv.True(total);
                            }
                            else
                            {
                                logger.Info($"{site}获取总数失败");
                                rv.False($"{site}获取总数失败");
                            }
                            break;
                        }
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

        /// <summary>
        /// 获取woocommerce的商品评论
        /// </summary>
        /// <param name="site"></param>
        /// <param name="wcKey"></param>
        /// <param name="wcSecret"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<string>> WcProductReviews(string site, string wcKey, string wcSecret, int productId, int page = 1, int pageSize = 10)
        {
            ReturnValue<string> rv = new ReturnValue<string>();
            var options = new RestClientOptions(site);
            var client = new RestClient(options);
            var request = new RestRequest("/wp-json/wc/v3/products/reviews", Method.Get);
            request.AddQueryParameter("page", page);
            request.AddQueryParameter("per_page", pageSize);
            request.AddQueryParameter("product", productId);
            // 设置 Basic Auth
            var authBytes = Encoding.UTF8.GetBytes($"{wcKey}:{wcSecret}");
            var authBase64 = Convert.ToBase64String(authBytes);
            request.AddHeader("Authorization", $"Basic {authBase64}");
            RestResponse response = await client.ExecuteAsync(request);
            if (response.IsSuccessStatusCode)
            {
                if (!string.IsNullOrWhiteSpace(response.Content) && (response.Content.StartsWith("{") || response.Content.StartsWith("[")))
                {
                    rv.True(response.Content);
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

        /// <summary>
        /// 获取woocommerce的商品评论数量
        /// </summary>
        /// <param name="site"></param>
        /// <param name="wcKey"></param>
        /// <param name="wcSecret"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<int>> WcProductReviewsTotal(string site, string wcKey, string wcSecret, int productId)
        {
            ReturnValue<int> rv = new ReturnValue<int>();
            var options = new RestClientOptions(site);
            var client = new RestClient(options);
            var request = new RestRequest("/wp-json/wc/v3/products/reviews", Method.Get);
            request.AddQueryParameter("page", 1);
            request.AddQueryParameter("per_page", 1);
            request.AddQueryParameter("product", productId);
            // 设置 Basic Auth
            var authBytes = Encoding.UTF8.GetBytes($"{wcKey}:{wcSecret}");
            var authBase64 = Convert.ToBase64String(authBytes);
            request.AddHeader("Authorization", $"Basic {authBase64}");
            RestResponse response = await client.ExecuteAsync(request);
            if (response.IsSuccessStatusCode)
            {
                if (!string.IsNullOrWhiteSpace(response.Content) && (response.Content.StartsWith("{") || response.Content.StartsWith("[")))
                {
                    foreach (var header in response.Headers)
                    {
                        if (header.Name.Equals("X-WP-Total", StringComparison.OrdinalIgnoreCase))
                        {
                            if (int.TryParse(header.Value.ToString(), out int total))
                            {
                                rv.True(total);
                            }
                            else
                            {
                                logger.Info("获取总数失败");
                                rv.False("获取总数失败");
                            }
                            break;
                        }
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

        /// <summary>
        /// 创建评论
        /// </summary>
        /// <param name="site"></param>
        /// <param name="wcKey"></param>
        /// <param name="wcSecret"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<string>> WcReviewCreate(string site, string wcKey, string wcSecret, int productId, string name, string email, string content, int rating)
        {
            ReturnValue<string> rv = new ReturnValue<string>();
            var options = new RestClientOptions(site);
            var client = new RestClient(options);
            var request = new RestRequest("/wp-json/wc/v3/products/reviews", Method.Post);
            // 设置 Basic Auth
            var authBytes = Encoding.UTF8.GetBytes($"{wcKey}:{wcSecret}");
            var authBase64 = Convert.ToBase64String(authBytes);
            request.AddHeader("Authorization", $"Basic {authBase64}");

            request.AddHeader("Content-Type", "application/json");
            var body = new
            {
                product_id = productId,
                review = content,
                reviewer = name,
                reviewer_email = email,
                rating = rating
            };
            request.AddJsonBody(body);

            RestResponse response = await client.ExecuteAsync(request);
            if (response.IsSuccessStatusCode)
            {
                if (!string.IsNullOrWhiteSpace(response.Content) && (response.Content.StartsWith("{") || response.Content.StartsWith("[")))
                {
                    rv.True(response.Content);
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

        /// <summary>
        /// 创建评论
        /// </summary>
        /// <param name="site"></param>
        /// <param name="wcKey"></param>
        /// <param name="wcSecret"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<string>> WcReviewDelete(string site, string wcKey, string wcSecret, int reviewId)
        {
            ReturnValue<string> rv = new ReturnValue<string>();
            var options = new RestClientOptions(site);
            var client = new RestClient(options);
            var request = new RestRequest($"/wp-json/wc/v3/products/reviews/{reviewId}", Method.Delete);
            // 设置 Basic Auth
            var authBytes = Encoding.UTF8.GetBytes($"{wcKey}:{wcSecret}");
            var authBase64 = Convert.ToBase64String(authBytes);
            request.AddHeader("Authorization", $"Basic {authBase64}");

            RestResponse response = await client.ExecuteAsync(request);
            if (response.IsSuccessStatusCode)
            {
                if (!string.IsNullOrWhiteSpace(response.Content) && (response.Content.StartsWith("{") || response.Content.StartsWith("[")))
                {
                    rv.True(response.Content);
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
    }
}
