using AIText.Models.SendRecord;
using Azure;
using Dm;
using Entitys;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using NPOI.HSSF.Record.Chart;
using NPOI.SS.UserModel;
using NPOI.XWPF.UserModel;
using Org.BouncyCastle.Crypto;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace AIText.Controllers
{
    public class SendRecordController : BaseController
    {
        static Random RecordRandom = new Random();
        private readonly SqlSugarClient Db;
        private readonly IWebHostEnvironment _env;
        public SendRecordController(SqlSugarClient _Db, IWebHostEnvironment env)
        {
            Db = _Db;
            _env = env;
        }
        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> DoListAsync(SendRecordSearch search)
        {
            await TryUpdateModelAsync(search.Pager);
            var query = SearchSql(search);
            var pageList = new commons.util.PageList<SendRecordDto>();
            int count = 0;
            pageList.List = query.Select<SendRecordDto>().ToPageList(search.Pager.PageIndex, search.Pager.PageSize, ref count);
            pageList.PagerModel = new commons.util.PageModel()
            {
                PageSize = search.Pager.PageSize,
                PageIndex = search.Pager.PageIndex,
                Count = count
            };
            return PartialView(pageList);
        }
        public IActionResult Add()
        {
            var setting = Db.Queryable<SiteAccount>().Where(o => o.IsEnable == true && o.StartDate <= DateTime.Now).ToList();
            ViewData["Sites"] = setting;
            return PartialView(new SendRecord());
        }

        /// <summary>
        /// 创建记录
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public IActionResult DoAdd(string Id)
        {
            var rv = new ReturnValue<string>();
            var setting = Db.Queryable<SiteAccount>().Where(o => o.Id == Id).First();
            if (setting != null)
            {
                // 没使用过的，最少使用量的关键词
                var siteKeyword = Db.Queryable<SiteKeyword>().Where(o => o.SiteId == setting.Id).OrderBy(o => o.UseCount).First();

                if (siteKeyword == null)
                {
                    rv.False("没有设置站点关键词");
                    return Json(rv);
                }

                // 抽取指令
                var promptTempList = Db.Queryable<PromptTemplate>().ToList();
                var promptTemp = promptTempList[RecordRandom.Next(0, promptTempList.Count - 1)];

                var urlList = Db.Queryable<SiteKeyword>().Where(o => o.SiteId == setting.Id)
                    .Select(o => o.URL)
                    .OrderBy(o => SqlFunc.GetRandom()).Take(3).ToList();

                var urlString = string.Join(",", urlList);

                SendRecord sendRecord = new SendRecord
                {
                    Link = urlString,
                    KeywordId = siteKeyword.Id,
                    Keyword = siteKeyword.Keyword,
                    TemplateId = promptTemp.Id,
                    TemplateName = promptTemp.Name,
                    IsSync = false,
                    SyncSiteId  = setting.Id,
                    SyncSite = setting.Site,
                    SyncTime = null,
                    CreateTime = DateTime.Now,
                    UpdateTime = null
                };
                var ret = Db.Insertable(sendRecord).ExecuteCommand();
                rv.status = ret > 0;
                if (rv.status)
                {
                    Db.Updateable<SiteKeyword>().SetColumns(o => o.UseCount == (o.UseCount +1)).Where(o => o.Id == siteKeyword.Id).ExecuteCommand();
                }
            }
            return Json(rv);
        }

        /// <summary>
        /// 绘图
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public async Task<IActionResult> DoDraw(int Id)
        {
            var rv = new ReturnValue<string>();

            var sendRecord = Db.Queryable<SendRecord>().Where(o => o.Id == Id).First();
            // TODO: 使用已有资源图片，或是创建图片
            var paintAccount = Db.Queryable<PaintAccount>().Where(o => o.IsEnable == true).First();
            string imgUrl = string.Empty;
            var imgResult = await Liblibai.Text2img(paintAccount.AccessKey, paintAccount.SecretKey, $"高清产品图，{sendRecord.Keyword}");
            DateTime? imgTime = null;
            if (imgResult.Contains("|"))
            {
                // 生成图片出错写库
                var msg = imgResult.Substring(imgResult.IndexOf("|"));
                Db.Updateable<SendRecord>()
                            .SetColumns(o => o.ErrMsg == msg)
                            .SetColumns(o => o.ImgTime == DateTime.Now)
                            .Where(o => o.Id == Id).ExecuteCommand();
                rv.False("生成图片出错" + msg);
                return Json(rv);
            }
            else
            {
                rv.status = await GetImgResult(paintAccount, imgResult, Id);
                return Json(rv);
            }
        }

        private async Task<bool> GetImgResult(PaintAccount paintAccount, string uuid, int id, int retry = 0)
        {
            var imgUrlResult = await Liblibai.status(paintAccount.AccessKey, paintAccount.SecretKey, uuid);
            if (imgUrlResult.Contains("|"))
            {
                if (retry < 3)
                {
                    await Task.Delay(3000 * (retry + 1));
                    return await GetImgResult(paintAccount, uuid, id, ++retry);
                }
                else
                {
                    var msg = imgUrlResult.Substring(imgUrlResult.IndexOf("|"));
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
                //var imgs = imgUrlResult.Split(",", StringSplitOptions.RemoveEmptyEntries);
                //List<string> downImgs = new List<string>();
                //foreach (var url in imgs)
                //{
                //    var path = DownloadImg(url);
                //    if (!string.IsNullOrEmpty(path))
                //        downImgs.Add(path);
                //}
                //imgPaths = string.Join(",", downImgs);
                // 更新图片地址
                var ret = Db.Updateable<SendRecord>()
                            .SetColumns(o => o.ImgUrl == imgUrlResult)
                            .SetColumns(o => o.ImgPath == imgPaths)
                            .SetColumns(o => o.ImgTime == DateTime.Now)
                            .Where(o => o.Id == id).ExecuteCommand();
                return ret > 0;
            }
        }

        public IActionResult DoDown(int Id)
        {
            ReturnValue<string> rv = new ReturnValue<string>();
            var sendRecord = Db.Queryable<SendRecord>().Where(o => o.Id == Id).First();
            var imgs = sendRecord.ImgUrl.Split(",", StringSplitOptions.RemoveEmptyEntries);
            List<string> downImgs = new List<string>();
            foreach (var url in imgs)
            {
                Thread.Sleep(5000);
                var path = DownloadImg(url);
                if (!string.IsNullOrEmpty(path))
                    downImgs.Add(path);
            }
            string imgPaths = string.Join(",", downImgs);
            // 更新图片地址
            var ret = Db.Updateable<SendRecord>()
                        .SetColumns(o => o.ImgPath == imgPaths)
                        .SetColumns(o => o.ImgTime == DateTime.Now)
                        .Where(o => o.Id == Id).ExecuteCommand();
            rv.status = ret > 0;
            return Json(rv);
        }

        private string DownloadImg(string url)
        {
            // 图片记录地址，是个URL记录，进行图片下载，设置到单元格内后，url文本内容会被图片遮挡住
            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    var path = Path.Combine("Resources", "Images", Path.GetFileName(url));
                    string savePath = Path.Combine(_env.WebRootPath, path);
                    if (DownloadFile(url, savePath))
                        return path;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            return null;
        }

        static bool DownloadFile(string fileUrl, string savePath)
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    // 获取文件所在的目录路径
                    string? directoryPath = Path.GetDirectoryName(savePath);

                    // 如果目录不存在，则创建
                    if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
                    {
                        Directory.CreateDirectory(directoryPath);
                    }

                    client.DownloadFile(fileUrl, savePath);
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            }
        }

        public async Task<IActionResult> DoAI(int Id)
        {
            var rv = new ReturnValue<string>();

            var sendRecord = Db.Queryable<SendRecord>().Where(o => o.Id == Id).First();

            if (!string.IsNullOrEmpty(sendRecord.Content)) 
            {
                //string content1 = sendRecord.Content;
                //// TODO: 从文章中提取标题
                //// 使用正则提取 <h1> 标签中的内容
                //Match titleMatch = Regex.Match(content1, @"<h1>(.*?)<\/h1>", RegexOptions.Singleline);
                //string title = titleMatch.Success ? titleMatch.Groups[1].Value : "No Title";

                //// 移除 <h1> 标签，留下正文
                //string body = Regex.Replace(content1, @"<h1>.*?<\/h1>", "", RegexOptions.Singleline).Trim();
                //// 更新文章
                //var ret = Db.Updateable<SendRecord>()
                //            .SetColumns(o => o.Title == title)  // TODO: 从文章中提取标题
                //            .SetColumns(o => o.Content == body)
                //            .Where(o => o.Id == Id).ExecuteCommand();
                //rv.status = ret > 0;
                //return Json(rv);

                rv.False("文章已经生成");
                return Json(rv);
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

            var content = await Volcengine.ChatCompletions(aiAccount.ApiKey, prompt);
            if (content.Contains("|"))
            {
                string msg = content.Substring(content.IndexOf("|"));
                // 更新文章
                var ret = Db.Updateable<SendRecord>()
                            .SetColumns(o => o.AiSiteId == aiAccount.Id)
                            .SetColumns(o => o.AiSite == aiAccount.Site)
                            .SetColumns(o => o.Prompt == prompt)
                            .SetColumns(o => o.ErrMsg == msg)
                            .SetColumns(o => o.AiTime == DateTime.Now)
                            .Where(o => o.Id == Id).ExecuteCommand();

                rv.False("生成文章出错" + msg);
                return Json(rv);
            }
            else
            {
                // 从文章中提取标题
                (string title, string body) = ExtractTitleAndBody(content);

                if (!string.IsNullOrEmpty(sendRecord.ImgPath))
                {
                    string baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
                    string webPath = sendRecord.ImgPath.Replace('\\', '/').TrimStart('/');
                    body.Replace(sendRecord.ImgUrl, $"{baseUrl}/{webPath}");
                }
                // 更新文章
                var ret = Db.Updateable<SendRecord>()
                            .SetColumns(o => o.AiSiteId == aiAccount.Id)
                            .SetColumns(o => o.AiSite == aiAccount.Site)
                            .SetColumns(o => o.Prompt == prompt)
                            .SetColumns(o => o.Title == title)
                            .SetColumns(o => o.Content == body)
                            .SetColumns(o => o.AiTime == DateTime.Now)
                            .Where(o => o.Id == Id).ExecuteCommand();
                rv.status = ret > 0;
                return Json(rv);
            }
        }

        public IActionResult DoTitle(int Id)
        {
            ReturnValue<string> rv = new ReturnValue<string>();
            var sendRecord = Db.Queryable<SendRecord>().Where(o => o.Id == Id).First();


            // 从文章中提取标题
            (string title, string body) = ExtractTitleAndBody(sendRecord.Content);

            if (!string.IsNullOrEmpty(sendRecord.ImgPath))
            {
                string baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
                string webPath = sendRecord.ImgPath.Replace('\\', '/').TrimStart('/');
                body.Replace(sendRecord.ImgUrl, $"{baseUrl}/{webPath}");
            }
            // 更新文章
            var ret = Db.Updateable<SendRecord>()
                        .SetColumns(o => o.Title == title)
                        .SetColumns(o => o.Content == body)
                        .Where(o => o.Id == Id).ExecuteCommand();
            rv.status = ret > 0;
            return Json(rv);
        }


        static (string title, string body) ExtractTitleAndBody(string content)
        {
            // 正则匹配 <h1>、<h2>、<h3> 中的第一个
            Match titleMatch = Regex.Match(content, @"<(h[1-3])>(.*?)<\/\1>", RegexOptions.Singleline | RegexOptions.IgnoreCase);

            string title = "No Title";
            if (titleMatch.Success)
            {
                title = titleMatch.Groups[2].Value.Trim(); // 提取标题文本
                content = content.Replace(titleMatch.Value, "").Trim(); // 移除标题
            }

            return (title, content);
        }

        public async Task<IActionResult> DoSync(int Id)
        {
            var rv = new ReturnValue<string>();
            var sendRecord = Db.Queryable<SendRecord>().Where(o => o.Id == Id).First();
            if (sendRecord.IsSync == true)
            {
                rv.False("文章已经同步");
                return Json(rv);
            }
            if (string.IsNullOrEmpty(sendRecord.Content))
            {
                rv.False("文章未生成");
                return Json(rv);
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
                        string token = await WordpressApi.GetAccessToken(syncAccount.Site, syncAccount.Username, syncAccount.Password);
                        if (token.Contains("|"))
                        {
                            string msg = token.Substring(token.IndexOf("|"));
                            Db.Updateable<SendRecord>().SetColumns(o => o.SyncErrMsg == msg).Where(o => o.Id == sendRecord.Id).ExecuteCommand();
                            rv.False("获取WP站点的token出错");
                            return Json(rv);
                        }
                        syncAccount.AccessKey = token;
                        Db.Updateable<SiteAccount>().SetColumns(o => o.AccessKey == token).Where(o => o.Id == syncAccount.Id).ExecuteCommand();
                    }
                    var sendRes = await WordpressApi.PostToCreate(syncAccount.Site, syncAccount.AccessKey, sendRecord.Title, sendRecord.Content);
                    if (sendRes.Contains("|"))
                    {
                        // JWT 无效时，重新获取JWT
                        if (sendRes.StartsWith("403|") || sendRes.StartsWith("401|"))
                        {
                            string token = await WordpressApi.GetAccessToken(syncAccount.Site, syncAccount.Username, syncAccount.Password);
                            if (token.Contains("|"))
                            {
                                string msg1 = token.Substring(token.IndexOf("|"));
                                Db.Updateable<SendRecord>().SetColumns(o => o.SyncErrMsg == msg1).Where(o => o.Id == sendRecord.Id).ExecuteCommand();
                                rv.False("获取WP站点的token出错");
                                return Json(rv);
                            }
                            syncAccount.AccessKey = token;
                            Db.Updateable<SiteAccount>().SetColumns(o => o.AccessKey == token).Where(o => o.Id == syncAccount.Id).ExecuteCommand();
                            sendRes = await WordpressApi.PostToCreate(syncAccount.Site, syncAccount.AccessKey, sendRecord.Title, sendRecord.Content);
                            if (sendRes.Contains("|"))
                            {
                                string msg2 = sendRes.Substring(sendRes.IndexOf("|"));
                                rv.False("发送失败" + msg2);
                                return Json(rv);
                            }
                            else
                            {
                                rv.status = UpdateSyncResult(sendRes, sendRecord.Id);
                                return Json(rv);
                            }
                        }
                        else
                        {
                            string msg = sendRes.Substring(sendRes.IndexOf("|"));
                            Db.Updateable<SendRecord>().SetColumns(o => o.SyncErrMsg == msg).Where(o => o.Id == sendRecord.Id).ExecuteCommand();
                            rv.False("发送出错");
                            return Json(rv);
                        }
                    }
                    else
                    {
                        rv.status = UpdateSyncResult(sendRes, sendRecord.Id);
                        return Json(rv);
                    }
                }
                else
                {
                    rv.False("站点类型不支持");
                    return Json(rv);
                }
            }
            else
            {
                rv.False("同步站点不存在");
                return Json(rv);
            }
        }

        private bool UpdateSyncResult(string sendRes, int Id)
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
                    //
                }
            }
            var ret = Db.Updateable<SendRecord>()
                        .SetColumns(o => new SendRecord { IsSync = true, SyncUrl = syncUrl, SyncTime = DateTime.Now })
                        .Where(o => o.Id == Id)
                        .ExecuteCommand();
            return ret > 0;

        }

        public IActionResult Preview(int Id)
        {
            var model = Db.Queryable<SendRecord>().Where(t => t.Id == Id).First();
            return PartialView(model);
        }


        public IActionResult Detail(int Id)
        {
            var model = Db.Queryable<SendRecord>().Where(t => t.Id == Id).First();
            return View(model);
        }

        public IActionResult DoDelete(int Id)
        {
            var rv = new ReturnValue<string>();
            Db.Deleteable<SendRecord>().Where(it => it.Id == Id).ExecuteCommand();
            rv.True("删除完成");
            return Json(rv);
        }
        private ISugarQueryable<SendRecord> SearchSql(SendRecordSearch search)
        {
            var query = Db.Queryable<SendRecord>();
            if (!string.IsNullOrEmpty(search.Prompt))
            {
                query.Where(t => t.Prompt.Contains(search.Prompt));
            }

            return query;
        }
    }
}
