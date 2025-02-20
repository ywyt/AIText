using AIText.Models.SendRecord;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Threading.Tasks;
using System;

namespace AIText.Controllers
{
    public class SendRecordController : BaseController
    {
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
            return PartialView(new SendRecord());
        }
        public IActionResult DoAdd(SendRecord add)
        {
            var rv = new ReturnValue<string>();
            var now = DateTime.Now;
            add.CreateTime = now;
            add.UpdateTime = now;
            var num = Db.Insertable<SendRecord>(add).ExecuteCommand();
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
        public IActionResult Edit(int Id)
        {
            var model = Db.Queryable<SendRecord>().Where(t => t.Id == Id).First();
            return PartialView(model);
        }
        public IActionResult DoEdit(SendRecord edit)
        {
            var rv = new ReturnValue<string>();
            var model = Db.Queryable<SendRecord>().Where(t => t.Id == edit.Id).First();
            edit.Prompt = model.Prompt;
            edit.Title = model.Title;
            edit.Content = model.Content;
            edit.IsSync = model.IsSync;
            edit.SyncSite = model.SyncSite;
            edit.SyncTime = model.SyncTime;
            edit.UpdateTime = DateTime.Now;
            var num = Db.Updateable<SendRecord>(edit).ExecuteCommand();
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
