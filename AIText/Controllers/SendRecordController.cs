using AIText.Models.SendRecord;
using Dm;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NPOI.XWPF.UserModel;
using SqlSugar;
using System;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AIText.Controllers
{
    public class SendRecordController : BaseController
    {
        static Random RecordRandom = new Random();
        private readonly SqlSugarClient Db;
        public SendRecordController(SqlSugarClient _Db)
        {
            Db = _Db;
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

                // 抽取指令
                var promptTempList = Db.Queryable<PromptTemplate>().ToList();
                var promptTemp = promptTempList[RecordRandom.Next(0, promptTempList.Count - 1)];
                
                SendRecord sendRecord = new SendRecord
                {
                    SettingId = Id,
                    //AiSiteId = setting.AiSiteId,
                    //AiSite = setting.AiSite,
                    Link = siteKeyword.URL,
                    KeywordId = siteKeyword.Id,
                    Keyword = siteKeyword.Keyword,
                    TemplateId = promptTemp.Id,
                    IsSync = false,
                    SyncSiteId  = setting.Id,
                    SyncSite = setting.Site,
                    SyncTime = null,
                    CreateTime = DateTime.Now,
                    UpdateTime = null
                };
                var ret = Db.Insertable(sendRecord).ExecuteCommand();
                rv.status = ret > 0;
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
                imgUrl = await Liblibai.status(paintAccount.AccessKey, paintAccount.SecretKey, imgResult);
                imgTime = DateTime.Now;
                // TODO: 将图片下载保存本地，后续替换content中的imgUrl
                //ImgPath = "", 

                // 更新图片地址
                var ret = Db.Updateable<SendRecord>()
                            .SetColumns(o => o.ImgUrl == imgUrl)
                            .SetColumns(o => o.ImgTime == imgTime)
                            .Where(o => o.Id == Id).ExecuteCommand();
                rv.status = ret > 0;
                return Json(rv);
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
            prompt += $" 需要在文章中选择合适的文字插入链接{sendRecord.Link}";
            if (!string.IsNullOrEmpty(sendRecord.ImgUrl))
            {
                prompt += $" 在文章的段落中插入图片{sendRecord.ImgUrl}";
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
                // 使用正则提取 <h1> 标签中的内容
                Match titleMatch = Regex.Match(content, @"<h1>(.*?)<\/h1>", RegexOptions.Singleline);
                string title = titleMatch.Success ? titleMatch.Groups[1].Value : "No Title";

                // 移除 <h1> 标签，留下正文
                string body = Regex.Replace(content, @"<h1>.*?<\/h1>", "", RegexOptions.Singleline).Trim();
                if (!string.IsNullOrEmpty(sendRecord.ImgPath))
                {
                    body.Replace(sendRecord.ImgUrl, sendRecord.ImgPath);
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
                                var ret = Db.Updateable<SendRecord>()
                                    .SetColumns(o => new SendRecord { IsSync = true, SyncTime = DateTime.Now })
                                    .Where(o => o.Id == sendRecord.Id)
                                    .ExecuteCommand();
                                rv.status = ret > 0;
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
                        var ret = Db.Updateable<SendRecord>()
                                    .SetColumns(o => new SendRecord { IsSync = true, SyncTime = DateTime.Now })
                                    .Where(o => o.Id == sendRecord.Id)
                                    .ExecuteCommand();
                        rv.status = ret > 0;
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

        public IActionResult Preview(int Id)
        {
            var model = Db.Queryable<SendRecord>().Where(t => t.Id == Id).First();
            return PartialView(model);
        }


        public IActionResult Edit(int Id)
        {
            var model = Db.Queryable<SendRecord>().Where(t => t.Id == Id).First();
            return PartialView(model);
        }
        public IActionResult DoEdit(SendRecord edit)
        {
            var rv = new ReturnValue<string>();
            var model = Db.Queryable<SendRecord>().Where(t => t.Id == edit.Id).First();
            model.Prompt = edit.Prompt;
            model.Title = edit.Title;
            model.Content = edit.Content;
            model.IsSync = edit.IsSync;
            model.SyncSite = edit.SyncSite;
            model.SyncTime = edit.SyncTime;
            model.UpdateTime = DateTime.Now;
            var num = Db.Updateable<SendRecord>(model).ExecuteCommand();
            if (num == 1)
            {
                rv.True("修改成功");
                return Json(rv);
            }
            else
            {
                rv.False("操作失败");
                return Json(rv);
            }
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
