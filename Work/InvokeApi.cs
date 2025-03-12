using Entitys;
using NLog;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Work
{
    public class InvokeApi
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static string BaseUrl = "";

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
                Db.Updateable<SendRecord>()
                            .SetColumns(o => o.ImgErrMsg == msg)
                            .SetColumns(o => o.ImgTime == DateTime.Now)
                            .Where(o => o.Id == sendRecord.Id).ExecuteCommand();
                rv.False("生成图片出错" + msg);
                return rv;
            }
            else
            {
                logger.Info($"获取到哩布绘图的UUID: {imgResult.value}");
                await Task.Delay(5000);
                rv.status = await GetImgResult(Db, paintAccount, imgResult.value, sendRecord.Id);
                return rv;
            }
        }

        private static async Task<bool> GetImgResult(SqlSugarClient Db, PaintAccount paintAccount, string uuid, int id, int retry = 0)
        {
            var imgUrlResult = await Liblibai.GetImage(paintAccount.AccessKey, paintAccount.SecretKey, uuid);
            if (!imgUrlResult.status)
            {
                if (retry < 3)
                {
                    await Task.Delay(3000 * (retry + 1));
                    return await GetImgResult(Db, paintAccount, uuid, id, ++retry);
                }
                else
                {
                    var msg = imgUrlResult.errordetailed ?? imgUrlResult.errorsimple;
                    // 更新错误信息
                    var ret = Db.Updateable<SendRecord>()
                                .SetColumns(o => o.ImgErrMsg == msg)
                                .SetColumns(o => o.ImgTime == DateTime.Now)
                                .Where(o => o.Id == id).ExecuteCommand();
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
                // 更新图片地址
                var ret = Db.Updateable<SendRecord>()
                            .SetColumns(o => o.ImgUrl == imgUrlResult.value)
                            .SetColumns(o => o.ImgPath == imgPaths)
                            .SetColumns(o => o.ImgTime == DateTime.Now)
                            .Where(o => o.Id == id).ExecuteCommand();
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
                var ret = Db.Updateable<SendRecord>()
                            .SetColumns(o => o.AiSiteId == aiAccount.Id)
                            .SetColumns(o => o.AiSite == aiAccount.Site)
                            .SetColumns(o => o.Prompt == prompt)
                            .SetColumns(o => o.ErrMsg == msg)
                            .SetColumns(o => o.AiTime == DateTime.Now)
                            .Where(o => o.Id == sendRecord.Id).ExecuteCommand();

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
                // 更新文章
                var ret = Db.Updateable<SendRecord>()
                            .SetColumns(o => o.AiSiteId == aiAccount.Id)
                            .SetColumns(o => o.AiSite == aiAccount.Site)
                            .SetColumns(o => o.Prompt == prompt)
                            .SetColumns(o => o.Title == title)
                            .SetColumns(o => o.Content == body)
                            .SetColumns(o => o.AiTime == DateTime.Now)
                            .Where(o => o.Id == sendRecord.Id).ExecuteCommand();
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
            if (syncAccount != null)
            {
                if (syncAccount.SiteType == SiteType.WordPress)
                {
                    // 没有获取JWT时，生成JWT
                    if (string.IsNullOrEmpty(syncAccount.AccessKey))
                    {
                        var tokenRes = await WordpressApi.GetAccessToken(syncAccount.Site, syncAccount.Username, syncAccount.Password);
                        if (!tokenRes.status)
                        {
                            string msg = tokenRes.errordetailed ?? tokenRes.errorsimple;
                            Db.Updateable<SendRecord>().SetColumns(o => o.SyncErrMsg == msg).Where(o => o.Id == sendRecord.Id).ExecuteCommand();
                            rv.False("获取WP站点的token出错");
                            return rv;
                        }
                        string token = tokenRes.value;
                        syncAccount.AccessKey = token;
                        Db.Updateable<SiteAccount>().SetColumns(o => o.AccessKey == token).Where(o => o.Id == syncAccount.Id).ExecuteCommand();
                    }
                    var sendRes = await WordpressApi.PostToCreate(syncAccount.Site, syncAccount.AccessKey, sendRecord.Title, sendRecord.Content);
                    if (!sendRes.status)
                    {
                        // JWT 无效时，重新获取JWT
                        if (sendRes.errorsimple.StartsWith("403|") || sendRes.errorsimple.StartsWith("401|"))
                        {
                            var tokenRes = await WordpressApi.GetAccessToken(syncAccount.Site, syncAccount.Username, syncAccount.Password);
                            if (!tokenRes.status)
                            {
                                string msg1 = tokenRes.errordetailed ?? tokenRes.errorsimple;
                                Db.Updateable<SendRecord>().SetColumns(o => o.SyncErrMsg == msg1).Where(o => o.Id == sendRecord.Id).ExecuteCommand();
                                rv.False("获取WP站点的token出错");
                                return rv;
                            }
                            syncAccount.AccessKey = tokenRes.value;
                            Db.Updateable<SiteAccount>().SetColumns(o => o.AccessKey == syncAccount.AccessKey).Where(o => o.Id == syncAccount.Id).ExecuteCommand();
                            sendRes = await WordpressApi.PostToCreate(syncAccount.Site, syncAccount.AccessKey, sendRecord.Title, sendRecord.Content);
                            if (!sendRes.status)
                            {
                                string msg2 = sendRes.errorsimple;
                                rv.False("发送失败" + msg2);
                                return rv;
                            }
                            else
                            {
                                rv.status = UpdateSyncResult(Db, sendRes.value, sendRecord.Id);
                                return rv;
                            }
                        }
                        else
                        {
                            string msg = sendRes.errorsimple;
                            Db.Updateable<SendRecord>().SetColumns(o => o.SyncErrMsg == msg).Where(o => o.Id == sendRecord.Id).ExecuteCommand();
                            rv.False("发送出错");
                            return rv;
                        }
                    }
                    else
                    {
                        rv.status = UpdateSyncResult(Db, sendRes.value, sendRecord.Id);
                        return rv;
                    }
                }
                else
                {
                    rv.False("站点类型不支持");
                    return rv;
                }
            }
            else
            {
                rv.False("同步站点不存在");
                return rv;
            }
        }

        private static bool UpdateSyncResult(SqlSugarClient Db, string sendRes, int Id)
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
                    Db.Updateable<SendRecord>()
                        .SetColumns(o => o.SyncErrMsg == sendRes)
                        .Where(o => o.Id == Id)
                        .ExecuteCommand();
                    logger.Error(ex);
                    logger.Info(sendRes);
                    return false;
                }
            }
            var ret = Db.Updateable<SendRecord>()
                        .SetColumns(o => new SendRecord { IsSync = true, SyncUrl = syncUrl, SyncTime = DateTime.Now })
                        .Where(o => o.Id == Id)
                        .ExecuteCommand();
            return ret > 0;

        }
    }
}
