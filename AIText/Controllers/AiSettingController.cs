using AIText.Models.AiSetting;
using AIText.Models.SendRecord;
using Microsoft.AspNetCore.Mvc;
using NetTaste;
using SqlSugar;
using System;
using System.Threading.Tasks;

namespace AIText.Controllers
{
    public class AiSettingController : BaseController
    {
        private readonly SqlSugarClient Db;
        public AiSettingController(SqlSugarClient _Db)
        {
            Db = _Db;
        }
        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> DoListAsync(AiSettingSearch search) 
        {
            await TryUpdateModelAsync(search.Pager);
            var query= SearchSql(search);
            var pageList = new commons.util.PageList<AiSettingDto>();
            int count = 0;
            pageList.List = query.Select<AiSettingDto>().ToPageList(search.Pager.PageIndex, search.Pager.PageSize, ref count);
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
            var aiList = Db.Queryable<AiAccount>().ToList();
            var wpList = Db.Queryable<SiteAccount>().ToList();
            ViewData["aiList"] = aiList;
            ViewData["wpList"] = wpList;
            return PartialView(new AiSetting());
        }
        public IActionResult DoAdd(AiSetting add)
        {
            var rv = new ReturnValue<string>();
            add.Id = Guid.NewGuid().ToString();
            var now = DateTime.Now;
            if (!string.IsNullOrEmpty(add.AiSiteId))
            {
                add.AiSite=Db.Queryable<AiAccount>().Where(t => t.Id == add.AiSiteId).First().Site;
            }
            if (!string.IsNullOrEmpty(add.WpSiteId))
            {
                add.WpSite = Db.Queryable<SiteAccount>().Where(t => t.Id == add.WpSiteId).First().Site;
            }
            add.CreateTime = now;
            add.UpdateTime = now;
            var num = Db.Insertable<AiSetting>(add).ExecuteCommand();
            if (num == 1)
            {
                rv.True("新建成功");
                return Json(rv);
            }
            else
            {
                rv.False("操作失败");
                return Json(rv);
            }
        }
        public IActionResult Edit(string Id)
        {
            var aiList = Db.Queryable<AiAccount>().ToList();
            var wpList = Db.Queryable<SiteAccount>().ToList();
            ViewData["aiList"] = aiList;
            ViewData["wpList"] = wpList;
            var model = Db.Queryable<AiSetting>().Where(t => t.Id == Id).First();
            return PartialView(model);
        }
        public IActionResult DoEdit(AiSetting edit)
        {
            var rv = new ReturnValue<string>();
            var model = Db.Queryable<AiSetting>().Where(t => t.Id == edit.Id).First();
            model.Prompt = edit.Prompt;
            model.CountPerDay= edit.CountPerDay;
            if (!string.IsNullOrEmpty(edit.AiSiteId))
            {
                model.AiSiteId = edit.AiSiteId;
                model.AiSite = Db.Queryable<AiAccount>().Where(t => t.Id == edit.AiSiteId).First().Site;
            }
            if (!string.IsNullOrEmpty(edit.WpSiteId))
            {
                model.WpSiteId = edit.WpSiteId;
                model.WpSite = Db.Queryable<SiteAccount>().Where(t => t.Id == edit.WpSiteId).First().Site;
            }
            model.StartDate= edit.StartDate;
            model.IsEnable= edit.IsEnable;
            model.UpdateTime=DateTime.Now;
            var num = Db.Updateable<AiSetting>(model).ExecuteCommand();
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
        public IActionResult DoDelete(string Id)
        {
            var rv = new ReturnValue<string>();
            Db.Deleteable<AiSetting>().Where(it => it.Id == Id).ExecuteCommand();
            rv.True("删除完成");
            return Json(rv);
        }
        private ISugarQueryable<AiSetting> SearchSql(AiSettingSearch search)
        {
            var query = Db.Queryable<AiSetting>();
            if (!string.IsNullOrEmpty(search.Prompt))
            {
                query.Where(t => t.Prompt.Contains(search.Prompt));
            }

            return query;
        }


        #region 生成文章，同步WP

        public async Task<IActionResult> SendAi(string Id)
        {
            var model = Db.Queryable<AiSetting>().First(o => o.Id == Id);

            if (!string.IsNullOrEmpty(model.AiSiteId))
            {
                var aimodel = Db.Queryable<AiAccount>().First(o => o.Id == model.AiSiteId);
                if (aimodel != null && aimodel.Site?.Contains("volce") == true)
                {
                    var content = await Volcengine.ChatCompletions(aimodel.ApiKey, model.Prompt);
                    if (content.Contains("|"))
                    {
                        return Ok("生成文章出错" + content.Substring(content.IndexOf("|")));
                    }
                    SendRecord sendRecord = new SendRecord
                    {
                        SettingId = model.Id,
                        Prompt = model.Prompt,
                        Title = "AI GEN",
                        Content = content,
                        IsSync = false,
                        SyncSite = model.WpSite,
                        SyncTime = null,
                        CreateTime = DateTime.Now,
                        UpdateTime = null
                    };
                    Db.Insertable(sendRecord).ExecuteCommand();
                }
            }
            return Ok();
        }

        public async Task<IActionResult> SyncWp(string Id)
        {
            var model = Db.Queryable<AiSetting>().First(o => o.Id == Id);

            if (model != null)
            {
                var record = Db.Queryable<SendRecord>().Where(o => o.IsSync == false).OrderBy(o => o.CreateTime).First();
                if (record != null)
                {
                    SiteAccount wp = Db.Queryable<SiteAccount>().First(o => o.Id == model.WpSiteId);
                    if (wp != null)
                    {
                        if (string.IsNullOrEmpty(wp.AccessKey))
                        {
                            string token = await WordpressApi.GetAccessToken(wp.Site, wp.Username, wp.Password);
                            if (token.Contains("|"))
                            {
                                return Ok("获取WP站点的token出错");
                            }
                            wp.AccessKey = token;
                            Db.Updateable<SiteAccount>().SetColumns(o => o.AccessKey == token).Where(o => o.Id == wp.Id).ExecuteCommand();
                        }
                        var sendRes = await WordpressApi.PostToCreate(wp.Site, wp.AccessKey, record.Title, record.Content);
                        if (sendRes.Contains("|"))
                        {
                            if (sendRes.StartsWith("403|") || sendRes.StartsWith("401|"))
                            {
                                string token = await WordpressApi.GetAccessToken(wp.Site, wp.Username, wp.Password);
                                if (token.Contains("|"))
                                {
                                    return Ok("获取WP站点的token出错");
                                }
                                wp.AccessKey = token;
                                Db.Updateable<SiteAccount>().SetColumns(o => o.AccessKey == token).Where(o => o.Id == wp.Id).ExecuteCommand();
                                sendRes = await WordpressApi.PostToCreate(wp.Site, wp.AccessKey, record.Title, record.Content);
                                if (sendRes.Contains("|"))
                                {
                                    return Ok("发送失败" + sendRes.Substring(sendRes.IndexOf("|")));
                                }
                                else
                                {
                                    Db.Updateable<SendRecord>().SetColumns(o => new SendRecord { IsSync = true, SyncTime = DateTime.Now }).Where(o => o.Id == record.Id).ExecuteCommand();
                                }
                            }
                        }
                        else
                        {
                            Db.Updateable<SendRecord>().SetColumns(o => new SendRecord { IsSync = true, SyncTime = DateTime.Now }).Where(o => o.Id == record.Id).ExecuteCommand();
                        }
                    }
                }
            }
            return Ok();
        }

        #endregion
    }
}
