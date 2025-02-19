using AIText.Models.AiSetting;
using AIText.Models.SendRecord;
using Microsoft.AspNetCore.Mvc;
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
            return PartialView(new AiSetting());
        }
        public IActionResult DoAdd(AiSetting add)
        {
            var rv = new ReturnValue<string>();
            add.Id = Guid.NewGuid().ToString();
            var now = DateTime.Now;
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
            var model = Db.Queryable<AiSetting>().Where(t => t.Id == Id).First();
            return PartialView(model);
        }
        public IActionResult DoEdit(AiSetting edit)
        {
            var rv = new ReturnValue<string>();
            var model = Db.Queryable<AiSetting>().Where(t => t.Id == edit.Id).First();
            edit.Prompt = model.Prompt;
            edit.CountPerDay=model.CountPerDay;
            edit.AiSite=model.AiSite ;
            edit.WpSite=model.WpSite  ;
            edit.StartDate=model.StartDate;
            edit.IsEnable=model.IsEnable;
            edit.UpdateTime=DateTime.Now;
            var num = Db.Updateable<AiSetting>(edit).ExecuteCommand();
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
    }
}
