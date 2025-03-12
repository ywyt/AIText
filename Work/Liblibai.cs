using Entitys;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using NLog;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace Work
{
    /// <summary>
    /// 哩布哩布
    /// </summary>
    public class Liblibai
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Alpha文生图
        /// </summary>
        /// <param name="accessKey"></param>
        /// <param name="secretKey"></param>
        /// <param name="prompt"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<string>> Text2img(string accessKey, string secretKey, string prompt)
        {
            ReturnValue<string> rv = new ReturnValue<string>();

            string uri = "/api/generate/webui/text2img/ultra";
            // 当前毫秒时间戳
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // 生成随机字符串
            string signatureNonce = GetSecureRandomAlphanumeric(10);
            // 生成签名
            string signature = MakeSign(secretKey, uri, timestamp, signatureNonce);

            var options = new RestClientOptions("https://openapi.liblibai.cloud");
            var client = new RestClient(options);

            //var fulluri = $"{uri}?AccessKey={accessKey}&Signature={signature}&Timestamp={timestamp}&SignatureNonce={signatureNonce}";

            var request = new RestRequest(uri, Method.Post);
            // 开通开放平台授权的访问AccessKey
            request.AddQueryParameter("AccessKey", accessKey);
            // 加密请求参数生成的签名
            request.AddQueryParameter("Signature", signature);
            // 生成签名时的毫秒时间戳，整数字符串，有效期5分钟
            request.AddQueryParameter("Timestamp", timestamp);
            // 生成签名时的随机字符串
            request.AddQueryParameter("SignatureNonce", signatureNonce);

            request.AddHeader("Content-Type", "application/json");
            //request.AddHeader("Authorization", $"Bearer {apikey}");

            var sendBody = new
            {
                templateUuid = "5d7e67009b344550bc1aa6ccbfa1d7f4", // 星流Star-3 Alpha文生图 模板UUID
                generateParams = new
                {
                    // "1 girl,lotus leaf,masterpiece,best quality,finely detail,highres,8k,beautiful and aesthetic,no watermark,"
                    prompt = prompt,
                    aspectRatio = "portrait",
                    //或者配置imageSize设置具体宽高
                    imageSize = new
                    {
                        width = 768,
                        height = 1024
                    },
                    imgCount = 1,
                    steps = 30, // 采样步数，建议30

                    //高级设置，可不填写
                    controlnet = new
                    {
                        controlType = "depth",
                        controlImage = "https://liblibai-online.liblib.cloud/img/081e9f07d9bd4c2ba090efde163518f9/7c1cc38e-522c-43fe-aca9-07d5420d743e.png",
                    }
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
                    string code = jsonResponse["code"];
                    if (!"0".Equals(code) && !"200".Equals(code))
                    {
                        string msg = code + "|" + response.ErrorMessage + jsonResponse["msg"];
                        logger.Info(msg);
                        rv.False(msg);
                    }
                    else
                    {
                        string generateUuid = jsonResponse["data"]["generateUuid"];
                        rv.True(generateUuid);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    logger.Info(response.Content);
                    rv.False("解析绘图结果出错");
                }
            }
            else
            {
                string msg = response.StatusCode + "|" + response.ErrorMessage + response.Content;
                logger.Info($"调用哩布绘图接口失败{msg}");
                rv.False(msg);
            }
            return rv;
        }

        /// <summary>
        /// 查询生图结果
        /// </summary>
        /// <param name="accessKey"></param>
        /// <param name="secretKey"></param>
        /// <param name="generateUuid"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<string>> GetImage(string accessKey, string secretKey, string generateUuid)
        {
            ReturnValue<string> rv = new ReturnValue<string>();

            string uri = "/api/generate/webui/status";

            // 当前毫秒时间戳
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            // 生成随机字符串
            string signatureNonce = GetSecureRandomAlphanumeric(10);
            // 生成签名
            string signature = MakeSign(secretKey, uri, timestamp, signatureNonce);

            var options = new RestClientOptions("https://openapi.liblibai.cloud");
            var client = new RestClient(options);

            //var fulluri = $"{uri}?AccessKey={accessKey}&Signature={signature}&Timestamp={timestamp}&SignatureNonce={signatureNonce}";

            var request = new RestRequest(uri, Method.Post);
            // 开通开放平台授权的访问AccessKey
            request.AddQueryParameter("AccessKey", accessKey);
            // 加密请求参数生成的签名
            request.AddQueryParameter("Signature", signature);
            // 生成签名时的毫秒时间戳，整数字符串，有效期5分钟
            request.AddQueryParameter("Timestamp", timestamp);
            // 生成签名时的随机字符串
            request.AddQueryParameter("SignatureNonce", signatureNonce);


            request.AddHeader("Content-Type", "application/json");
            //request.AddHeader("Authorization", $"Bearer {apikey}");

            var sendBody = new
            {
                generateUuid = generateUuid, // 生图任务uuid，发起生图任务时返回该字段
            };

            request.AddJsonBody(sendBody);
            RestResponse response = await client.ExecuteAsync(request);
            if (response.IsSuccessStatusCode)
            {
                try
                {
                    // 解析JSON响应
                    var jsonResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(response.Content);

                    // 提取content字段
                    string code = jsonResponse["code"];
                    if (!"0".Equals(code) && !"200".Equals(code))
                    {
                        string msg = code + "|" + response.ErrorMessage + jsonResponse["msg"];
                        logger.Info(msg);
                        rv.False(msg);
                    }
                    else
                    {
                        var imagesArray = jsonResponse["data"]["images"];
                        // 创建一个 List 来存储所有 imageUrl
                        List<string> imageUrlList = new List<string>();

                        // 遍历 images 数组
                        foreach (var image in imagesArray)
                        {
                            // 将每个 image 对象转换为 dynamic 类型
                            dynamic img = JsonConvert.DeserializeObject<dynamic>(image.ToString());

                            // 将 imageUrl 添加到 List 中
                            imageUrlList.Add(img.imageUrl.ToString());
                        }
                        rv.True(string.Join(",", imageUrlList));
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                    logger.Info(response.Content);
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="secretKey"></param>
        /// <param name="uri"></param>
        /// <param name="timestamp"></param>
        /// <param name="signatureNonce"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        static string MakeSign(string secretKey, string uri, long timestamp, string signatureNonce)
        {
            // 拼接请求数据
            string content = $"{uri}&{timestamp}&{signatureNonce}";

            try
            {
                using (HMACSHA1 mac = new HMACSHA1(Encoding.UTF8.GetBytes(secretKey)))
                {
                    byte[] hash = mac.ComputeHash(Encoding.UTF8.GetBytes(content));
                    return Base64UrlEncode(hash);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error generating signature", ex);
            }
        }

        static string GetSecureRandomAlphanumeric(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            char[] result = new char[length];
            byte[] buffer = new byte[length];

            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }

            for (int i = 0; i < length; i++)
            {
                result[i] = chars[buffer[i] % chars.Length];
            }

            return new string(result);
        }

        private static string Base64UrlEncode(byte[] input)
        {
            return Convert.ToBase64String(input)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
