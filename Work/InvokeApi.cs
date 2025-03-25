﻿using Entitys;
using NLog;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Work
{
    public class InvokeApi
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static string BaseUrl = "";
        public static readonly string SEOBaseURL = "https://sz0088.oss-cn-guangzhou.aliyuncs.com/image/SEO/";
        static List<PromptTemplate> promptTempList = new List<PromptTemplate>();
        static int promptTempIdx = 0;
        static List<string> styles = new List<string>();
        static List<string> colors = new List<string>();

        public static void Init(SqlSugarClient Db)
        {

            if (promptTempIdx > 1200)
            {
                promptTempIdx = 0;
            }

            // 可用的指令模板
            promptTempList = Db.Queryable<PromptTemplate>().Where(o => o.IsEnable == true).ToList();
            // 款式
            styles = Db.Queryable<ImageResource>().GroupBy(o => o.Style).Select(o => o.Style).ToList();
            // 颜色
            colors = Db.Queryable<ImageResource>().GroupBy(o => o.Color).Select(o => o.Color).ToList();
        }

        /// <summary>
        /// 创建记录
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="site"></param>
        /// <param name="sendRecord"></param>
        public static async Task<ReturnValue<SendRecord>> CreateRecord(SqlSugarClient Db, SiteAccount setting)
        {
            var rv = new ReturnValue<SendRecord>();
            if (setting == null)
                return rv;

            // 没使用过的，最少使用量的关键词
            var siteKeyword = Db.Queryable<SiteKeyword>().Where(o => o.SiteId == setting.Id).OrderBy(o => o.UseCount).First();

            if (siteKeyword == null)
            {
                rv.False("没有设置站点关键词");
                return rv;
            }

            // 抽取指令，多个站点轮流使用模板，也是一种随机（同理于随机播放的歌单）
            Interlocked.Increment(ref promptTempIdx);
            var promptTemp = promptTempList[promptTempIdx % promptTempList.Count];

            // 站点链接
            var urlList = Db.Queryable<SiteKeyword>().Where(o => o.SiteId == setting.Id)
                .Select(o => o.URL)
                .OrderBy(o => SqlFunc.GetRandom()).Take(3).ToList();

            var urlString = string.Join(",", urlList);

            // 选择图片
            var image = PickupImage(Db, siteKeyword.Keyword);
            // 判断图片路径中是否包含斜杠或反斜杠作为分隔符
            if (image.ImagePath.Contains("/") || image.ImagePath.Contains("\\"))
            {
                // 如果是反斜杠，统一转换为斜杠
                image.ImagePath = image.ImagePath.Replace("\\", "/");

                // 检查是否有额外的斜杠，去除多余的斜杠
                image.ImagePath = image.ImagePath.TrimStart('/');
            }

            // 使用Uri类来拼接完整的URL
            Uri baseURL = new Uri(SEOBaseURL);
            Uri fullURL = new Uri(baseURL, image.ImagePath);
            SendRecord sendRecord = new SendRecord
            {
                Link = urlString,
                KeywordId = siteKeyword.Id,
                Keyword = siteKeyword.Keyword,
                TemplateId = promptTemp.Id,
                TemplateName = promptTemp.Name,
                IsSync = false,
                SyncSiteId = setting.Id,
                SyncSite = setting.Site,
                SyncTime = null,
                CreateTime = DateTime.Now,
                ImgResourceId = image.Id,
                ImgUrl = fullURL.ToString()
            };
            var ret = Db.Insertable(sendRecord).ExecuteCommand();
            rv.status = ret > 0;
            if (rv.status)
            {
                rv.value = sendRecord;
                await Db.Updateable<SiteKeyword>().SetColumns(o => o.UseCount == (o.UseCount + 1)).Where(o => o.Id == siteKeyword.Id).ExecuteCommandAsync();
                await Db.Updateable<ImageResource>().SetColumns(o => o.UseCount == (o.UseCount + 1)).Where(o => o.Id == image.Id).ExecuteCommandAsync();
            }
            return rv;

        }

        public static ImageResource PickupImage(SqlSugarClient Db, string keyword)
        {
            // 包含该款式+颜色的
            if (styles.Any(o => keyword.Contains(o)) && colors.Any(o => keyword.Contains(o)))
            {
                // 选择最少使用的
                var image = Db.Queryable<ImageResource>().Where(o => keyword.Contains(o.Style) && keyword.Contains(o.Color)).OrderBy(o => o.UseCount).First();
                return image;
            }
            // 包含该款式的
            else if (styles.Any(o => keyword.Contains(o)))
            {
                // 选择最少使用的
                var image = Db.Queryable<ImageResource>().Where(o => keyword.Contains(o.Style)).OrderBy(o => o.UseCount).First();
                return image;
            }
            // 从所有的文件夹中抽取
            else
            {
                var image = Db.Queryable<ImageResource>().OrderBy(o => o.UseCount).OrderBy(o => SqlFunc.GetRandom()).First();
                return image;
            }

        }

        public static async Task<ReturnValue<string>> DoDraw(SqlSugarClient Db, int Id)
        {
            var sendRecord = Db.Queryable<SendRecord>().Where(o => o.Id == Id).First();
            if (sendRecord == null)
                return new ReturnValue<string> { errorsimple = "记录不存在" };
            return await DoDraw(Db, sendRecord);
        }

        public static async Task<ReturnValue<string>> DoDraw(SqlSugarClient Db, SendRecord sendRecord)
        {
            if (sendRecord == null)
                return new ReturnValue<string> { errorsimple = "记录不存在" };
            var rv = new ReturnValue<string>();
            var paintAccount = Db.Queryable<PaintAccount>().Where(o => o.IsEnable == true).First();
            string imgUrl = string.Empty;
            var imgResult = await Liblibai.Text2img(paintAccount.AccessKey, paintAccount.SecretKey, $"高清产品图，{sendRecord.Keyword}");
            if (!imgResult.status)
            {
                // 生成图片出错写库
                var msg = imgResult.errorsimple;
                await Db.Updateable<SendRecord>()
                            .SetColumns(o => o.ImgErrMsg == msg)
                            .SetColumns(o => o.ImgTime == DateTime.Now)
                            .Where(o => o.Id == sendRecord.Id).ExecuteCommandAsync();
                rv.False("生成图片出错" + msg);
                return rv;
            }
            else
            {
                logger.Info($"获取到哩布绘图的UUID: {imgResult.value}");
                await Task.Delay(5000);
                rv.status = await GetImgResult(Db, paintAccount, sendRecord, imgResult.value, sendRecord.Id);
                return rv;
            }
        }

        private static async Task<bool> GetImgResult(SqlSugarClient Db, PaintAccount paintAccount, SendRecord sendRecord, string uuid, int id, int retry = 0)
        {
            var imgUrlResult = await Liblibai.GetImage(paintAccount.AccessKey, paintAccount.SecretKey, uuid);
            if (!imgUrlResult.status)
            {
                if (retry < 3)
                {
                    await Task.Delay(3000 * (retry + 1));
                    return await GetImgResult(Db, paintAccount, sendRecord, uuid, id, ++retry);
                }
                else
                {
                    var msg = imgUrlResult.errordetailed ?? imgUrlResult.errorsimple;
                    // 更新错误信息
                    var ret = await Db.Updateable<SendRecord>()
                                .SetColumns(o => o.ImgErrMsg == msg)
                                .SetColumns(o => o.ImgTime == DateTime.Now)
                                .Where(o => o.Id == id).ExecuteCommandAsync();
                    return false;
                }
            }
            else
            {
                string imgPaths = null;
                // 下载图片
                //var imgs = imgUrlResult.Split(",", StringSplitOptions.RemoveEmptyEntries);
                //List<string> downImgs = new List<string>();
                //foreach (var url in imgs)
                //{
                //    var path = DownloadImg(url);
                //    if (!string.IsNullOrEmpty(path))
                //        downImgs.Add(path);
                //}
                //imgPaths = string.Join(",", downImgs);
                logger.Debug(imgUrlResult.value);
                sendRecord.ImgUrl = imgUrlResult.value;
                // 更新图片地址
                var ret = await Db.Updateable<SendRecord>()
                            .SetColumns(o => o.ImgUrl == imgUrlResult.value)
                            .SetColumns(o => o.ImgPath == imgPaths)
                            .SetColumns(o => o.ImgTime == DateTime.Now)
                            .Where(o => o.Id == id).ExecuteCommandAsync();
                return ret > 0;
            }
        }

        public static async Task<ReturnValue<string>> DoAI(SqlSugarClient Db, int Id)
        {
            var sendRecord = Db.Queryable<SendRecord>().Where(o => o.Id == Id).First();

            return await DoAI(Db, sendRecord);
        }

        public static async Task<ReturnValue<string>> DoAI(SqlSugarClient Db, SendRecord sendRecord)
        {
            if (sendRecord == null)
                return new ReturnValue<string> { errorsimple = "记录不存在" };

            var rv = new ReturnValue<string>();

            if (!string.IsNullOrEmpty(sendRecord.Content))
            {
                rv.False("文章已经生成");
                return rv;
            }

            // AI账号
            var aiAccount = Db.Queryable<AiAccount>().Where(o => o.IsEnable == true).First();
            var promptTemp = Db.Queryable<PromptTemplate>().Where(o => o.Id == sendRecord.TemplateId).First();

            // 组装指令
            var prompt = promptTemp.Prompt.Replace("{keyword}", sendRecord.Keyword);
            prompt += $"\n需要在文章中选择合适的文字插入链接{sendRecord.Link}";
            if (!string.IsNullOrEmpty(sendRecord.ImgUrl))
            {
                prompt += $"\n在文章的段落中插入图片{sendRecord.ImgUrl}";
            }

            var contentRes = await Volcengine.ChatCompletions(aiAccount.ApiKey, prompt);
            if (!contentRes.status)
            {
                string msg = contentRes.errordetailed ?? contentRes.errorsimple;
                // 更新文章
                var ret = await Db.Updateable<SendRecord>()
                            .SetColumns(o => o.AiSiteId == aiAccount.Id)
                            .SetColumns(o => o.AiSite == aiAccount.Site)
                            .SetColumns(o => o.Prompt == prompt)
                            .SetColumns(o => o.ErrMsg == msg)
                            .SetColumns(o => o.AiTime == DateTime.Now)
                            .Where(o => o.Id == sendRecord.Id).ExecuteCommandAsync();

                rv.False("生成文章出错" + msg);
                return rv;
            }
            else
            {
                // 从文章中提取标题
                (string title, string body) = ExtractTitleAndBody(contentRes.value);

                if (!string.IsNullOrEmpty(sendRecord.ImgPath))
                {
                    string webPath = sendRecord.ImgPath.Replace('\\', '/').TrimStart('/');
                    body.Replace(sendRecord.ImgUrl, $"{BaseUrl}/{webPath}");
                }
                sendRecord.Prompt = prompt;
                sendRecord.AiSiteId = aiAccount.Id;
                sendRecord.AiSite = aiAccount.Site;
                sendRecord.Prompt = prompt;
                sendRecord.Title = title;
                sendRecord.Content = body;
                // 更新文章
                var ret = await Db.Updateable<SendRecord>()
                            .SetColumns(o => o.AiSiteId == aiAccount.Id)
                            .SetColumns(o => o.AiSite == aiAccount.Site)
                            .SetColumns(o => o.Prompt == prompt)
                            .SetColumns(o => o.Title == title)
                            .SetColumns(o => o.Content == body)
                            .SetColumns(o => o.AiTime == DateTime.Now)
                            .Where(o => o.Id == sendRecord.Id).ExecuteCommandAsync();
                rv.status = ret > 0;
                return rv;
            }
        }

        private static (string title, string body) ExtractTitleAndBody(string content)
        {
            // 正则匹配 <h1>、<h2>、<h3> 中的第一个
            Match titleMatch = Regex.Match(content, @"<(h[1-3])>(.*?)<\/\1>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            string title = "No Title";
            if (titleMatch.Success)
            {
                title = titleMatch.Groups[2].Value.Trim(); // 提取标题文本
                content = content.Replace(titleMatch.Value, "").Trim(); // 移除标题
            }
            else
            {
                logger.Debug(content);
            }

            return (title, content);
        }

        public static async Task<ReturnValue<string>> DoSync(SqlSugarClient Db, int Id)
        {
            var sendRecord = Db.Queryable<SendRecord>().Where(o => o.Id == Id).First();
            return await DoSync(Db, sendRecord);
        }

        public static async Task<ReturnValue<string>> DoSync(SqlSugarClient Db, SendRecord sendRecord)
        {
            if (sendRecord == null)
                return new ReturnValue<string> { errorsimple = "记录不存在" };

            var rv = new ReturnValue<string>();
            if (sendRecord.IsSync == true)
            {
                rv.False("文章已经同步");
                return rv;
            }
            if (string.IsNullOrEmpty(sendRecord.Content))
            {
                rv.False("文章未生成");
                return rv;
            }
            // 同步站点
            var syncAccount = Db.Queryable<SiteAccount>().Where(o => o.Id == sendRecord.SyncSiteId).First();
            if (syncAccount == null)
            {
                rv.False("同步站点不存在");
                return rv;
            }
            if (syncAccount.SiteType != SiteType.WordPress)
            {
                rv.False("站点类型不支持");
                return rv;
            }

            // 没有获取JWT时，生成JWT
            if (string.IsNullOrEmpty(syncAccount.AccessKey))
            {
                var hasGotToken = await GenAccessToken(Db, syncAccount, sendRecord);
                if (!hasGotToken)
                {
                    rv.False("获取WP站点Token出错");
                    return rv;
                }
            }

            // 图片没有上传 TODO: 换个判定，wordpress路径 /uploads/
            if (sendRecord.Content.Contains(sendRecord.ImgUrl))
            {
                var upload = await UploadImg(Db, syncAccount, sendRecord);
                if (upload.status == false)
                {
                    rv.False("上传图片出错" + upload.errorsimple);
                    return rv;
                }
            }

            return await PublishArticle(Db, syncAccount, sendRecord);
        }

        public static async Task<ReturnValue<string>> UploadImg(SqlSugarClient Db, SiteAccount syncAccount, SendRecord sendRecord, int retry = 0)
        {
            var rv = new ReturnValue<string>();
            if (string.IsNullOrEmpty(sendRecord.ImgPath) && string.IsNullOrEmpty(sendRecord.ImgUrl))
            {
                rv.True("没有图片");
                return rv;
            }
            // 包含了图片链接（可能的上传路径）
            if (sendRecord.Content?.Contains("wp-image-") == true || sendRecord.Content?.Contains("/wp-content/uploads/") == true)
            {
                rv.True("已经上传过图片");
                return rv;
            }

            ReturnValue<string> uploadRes = new Entitys.ReturnValue<string>();
            if (!string.IsNullOrEmpty(sendRecord.ImgPath))
            {
                uploadRes = await WordpressApi.UploadImage(syncAccount.Site, syncAccount.AccessKey, sendRecord.ImgPath, sendRecord.Keyword);
            }
            else
            {
                using (HttpClient client = new HttpClient())
                {
                    try
                    {
                        // 发送 GET 请求并获取响应
                        HttpResponseMessage response = await client.GetAsync(sendRecord.ImgUrl);

                        // 确保响应成功
                        response.EnsureSuccessStatusCode();

                        // 获取文件名，优先从 Content-Disposition 响应头获取
                        string filename = GetFilenameFromContentDisposition(response.Content.Headers.ContentDisposition);

                        // 如果 Content-Disposition 中没有文件名，则从 URL 中提取
                        if (string.IsNullOrEmpty(filename))
                        {
                            filename = GetFilenameFromUrl(sendRecord.ImgUrl);
                        }

                        // 读取响应内容并返回 byte[]
                        byte[] imageBytes = await response.Content.ReadAsByteArrayAsync();
                        uploadRes = await WordpressApi.UploadImage(syncAccount.Site, syncAccount.AccessKey, imageBytes, sendRecord.Keyword, filename);
                    }
                    catch (Exception ex)
                    {
                        logger.Error("下载图片出错");
                        logger.Error(ex);
                        uploadRes.False("下载图片出错");
                    }
                }
            }

            if (!uploadRes.status)
            {
                // JWT 无效时，重新获取JWT
                if (uploadRes.errorsimple.StartsWith("403|") || uploadRes.errorsimple.StartsWith("401|"))
                {
                    if (retry < 1)
                    {
                        var genRes = await GenAccessToken(Db, syncAccount, sendRecord);
                        return await UploadImg(Db, syncAccount, sendRecord, ++retry);
                    }
                    else
                    {
                        logger.Info($"再度获取WP站点{syncAccount.Site}的Token失败{uploadRes.errorsimple}");
                        rv.False($"再度获取WP站点的Token失败{uploadRes.errorsimple}");
                        return rv;
                    }
                }
                else
                {
                    string msg = uploadRes.errorsimple;
                    await Db.Updateable<SendRecord>().SetColumns(o => o.SyncErrMsg == msg).Where(o => o.Id == sendRecord.Id).ExecuteCommandAsync();
                    rv.False("上传图片出错");
                    return rv;
                }
            }
            else
            {
                rv.True(uploadRes.value);
                if (!string.IsNullOrEmpty(sendRecord.Content) && sendRecord.Content.Contains(sendRecord.ImgUrl))
                {
                    sendRecord.Content = sendRecord.Content.Replace(sendRecord.ImgUrl, uploadRes.value);
                    await Db.Updateable<SendRecord>().SetColumns(o => o.Content == sendRecord.Content).Where(o => o.Id == sendRecord.Id).ExecuteCommandAsync();
                }

                return rv;
            }
        }

        /// <summary>
        /// 从Headers.ContentDisposition获取文件名
        /// </summary>
        /// <param name="contentDisposition"></param>
        /// <returns></returns>
        private static string GetFilenameFromContentDisposition(ContentDispositionHeaderValue contentDisposition)
        {
            if (contentDisposition != null)
            {
                // 优先使用 filename* 参数（RFC 5987）
                if (!string.IsNullOrEmpty(contentDisposition.FileNameStar))
                {
                    return contentDisposition.FileNameStar;
                }
                // 其次使用 filename 参数（RFC 2183）
                else if (!string.IsNullOrEmpty(contentDisposition.FileName))
                {
                    return contentDisposition.FileName;
                }
            }
            return null;
        }

        /// <summary>
        /// 从 URL 中提取文件名
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string GetFilenameFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            try
            {
                // 从 URL 中提取文件名
                Uri uri = new Uri(url);
                string path = uri.AbsolutePath;
                string filename = Path.GetFileName(path);
                return filename;
            }
            catch (UriFormatException)
            {
                // 处理无效的 URL
                Console.WriteLine("Invalid URL format.");
                return null;
            }
        }

        /// <summary>
        /// 发布文章
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="syncAccount">站点账号</param>
        /// <param name="sendRecord">记录</param>
        /// <param name="retry">重试次数</param>
        /// <returns></returns>
        private static async Task<ReturnValue<string>> PublishArticle(SqlSugarClient Db, SiteAccount syncAccount, SendRecord sendRecord, int retry = 0)
        {
            var rv = new ReturnValue<string>();
            var sendRes = await WordpressApi.PostToCreate(syncAccount.Site, syncAccount.AccessKey, sendRecord.Title, sendRecord.Content);
            if (!sendRes.status)
            {
                // JWT 无效时，重新获取JWT
                if (sendRes.errorsimple.StartsWith("403|") || sendRes.errorsimple.StartsWith("401|"))
                {
                    if (retry < 1)
                    {
                        var genRes = await GenAccessToken(Db, syncAccount, sendRecord);
                        return await PublishArticle(Db, syncAccount, sendRecord, ++retry);
                    }
                    else
                    {
                        logger.Info($"再度获取WP站点{syncAccount.Site}的Token失败{sendRes.errorsimple}");
                        rv.False($"再度获取WP站点的Token失败{sendRes.errorsimple}");
                        return rv;
                    }
                }
                else
                {
                    string msg = sendRes.errorsimple;
                    await Db.Updateable<SendRecord>().SetColumns(o => o.SyncErrMsg == msg).Where(o => o.Id == sendRecord.Id).ExecuteCommandAsync();
                    rv.False("发送出错");
                    return rv;
                }
            }
            else
            {
                rv.status = await UpdateSyncResult(Db, sendRes.value, sendRecord.Id);
                return rv;
            }
        }

        private static async Task<bool> GenAccessToken(SqlSugarClient Db, SiteAccount syncAccount, SendRecord sendRecord)
        {
            var tokenRes = await WordpressApi.GetAccessToken(syncAccount.Site, syncAccount.Username, syncAccount.Password);
            if (!tokenRes.status)
            {
                string msg = "获取WP站点的token出错" + tokenRes.errordetailed ?? tokenRes.errorsimple;
                await Db.Updateable<SendRecord>().SetColumns(o => o.SyncErrMsg == msg).Where(o => o.Id == sendRecord.Id).ExecuteCommandAsync();
                return false;
            }
            string token = tokenRes.value;
            syncAccount.AccessKey = token;
            await Db.Updateable<SiteAccount>().SetColumns(o => o.AccessKey == token).Where(o => o.Id == syncAccount.Id).ExecuteCommandAsync();
            return true;
        }

        private static async Task<bool> UpdateSyncResult(SqlSugarClient Db, string sendRes, int Id)
        {
            var syncUrl = string.Empty;
            if (!string.IsNullOrWhiteSpace(sendRes) && (sendRes.StartsWith("{") || sendRes.StartsWith("[")))
            {
                try
                {
                    var jsonResult = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(sendRes);
                    syncUrl = jsonResult["link"];
                }
                catch (Exception ex)
                {
                    await Db.Updateable<SendRecord>()
                        .SetColumns(o => o.SyncErrMsg == sendRes)
                        .Where(o => o.Id == Id)
                        .ExecuteCommandAsync();
                    logger.Error(ex);
                    logger.Info(sendRes);
                    return false;
                }
            }
            var ret = await Db.Updateable<SendRecord>()
                        .SetColumns(o => new SendRecord { IsSync = true, SyncUrl = syncUrl, SyncTime = DateTime.Now })
                        .Where(o => o.Id == Id)
                        .ExecuteCommandAsync();
            return ret > 0;

        }
    }
}
