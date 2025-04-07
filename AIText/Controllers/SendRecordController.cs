using AIText.Models.SendRecord;
using Azure;
using Dm;
using Entitys;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;
using Microsoft.JSInterop.Infrastructure;
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
using Work;

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
            var siteAccount = Db.Queryable<SiteAccount>().Where(o => o.IsEnable == true).ToList();
            ViewData["Site"] = siteAccount;
            var promptTemplate = Db.Queryable<PromptTemplate>().Where(o => o.IsEnable == true).ToList();
            ViewData["Prompt"] = promptTemplate;

            var search = new SendRecordSearch { BeginTime = DateTime.Now.Date };
            return View(search);
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

        public IActionResult ViewAiRecord(int Id)
        {
            var list = Db.Queryable<AiRecord>().Where(t => t.SendRecordId == Id).ToList();
            return View(list);
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
        public async Task<IActionResult> DoAdd(string Id)
        {
            var rv = new ReturnValue<SendRecord>();
            var setting = Db.Queryable<SiteAccount>().Where(o => o.Id == Id).First();
            if (setting != null)
            {
                rv = await InvokeApi.CreateRecord(Db, setting);
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
            var image = await InvokeApi.PickupImage(Db, sendRecord.Keyword);
            if (image != null)
            {
                // 判断图片路径中是否包含斜杠或反斜杠作为分隔符
                if (image.ImagePath.Contains("/") || image.ImagePath.Contains("\\"))
                {
                    // 如果是反斜杠，统一转换为斜杠
                    image.ImagePath = image.ImagePath.Replace("\\", "/");

                    // 检查是否有额外的斜杠，去除多余的斜杠
                    image.ImagePath = image.ImagePath.TrimStart('/');
                }

                // 使用Uri类来拼接完整的URL
                Uri baseURL = new Uri(InvokeApi.SEOBaseURL);
                Uri fullURL = new Uri(baseURL, image.ImagePath);
                string imageUrl = fullURL.ToString();
                await Db.Updateable<SendRecord>().SetColumns(o => o.ImgUrl == imageUrl).ExecuteCommandAsync();
                rv.True(imageUrl);
                return Json(rv);
            }
            else
            {
                rv.False("生成图片出错");
                return Json(rv);
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

            if (!string.IsNullOrEmpty(sendRecord.Content) && !string.IsNullOrEmpty(sendRecord.Title)) 
            {
                rv.False("文章已经生成");
                return Json(rv);
            }

            var aiResult = await InvokeApi.DoAI(Db, sendRecord);
            return Json(aiResult);
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
            var syncResult = await InvokeApi.DoSync(Db, Id);
            return Json(syncResult);
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
            if (!string.IsNullOrEmpty(search.SyncSiteId))
            {
                query.Where(t => t.SyncSiteId == search.SyncSiteId);
            }

            if (!string.IsNullOrEmpty(search.TemplateId))
            {
                query.Where(t => t.TemplateId == search.TemplateId);
            }

            if (!string.IsNullOrEmpty(search.Title))
            {
                query.Where(t => t.Title.Contains(search.Title));
            }

            if (search.BeginTime.HasValue)
            {
                query.Where(t => t.CreateTime >= search.BeginTime);
            }

            if (search.EndTime.HasValue)
            {
                query.Where(t => t.CreateTime <= search.EndTime);
            }

            query.OrderBy(t => t.CreateTime);

            return query;
        }
    }
}
