using AIText.Models.WpAccount;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Threading.Tasks;
using System;
using AIText;

namespace AIText.Controllers
{
    public class WpAccountController : BaseController
    {
        private readonly SqlSugarClient Db;
        public WpAccountController(SqlSugarClient _Db)
        {
            Db = _Db;
        }
        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> DoListAsync(WpAccountSearch search)
        {
            await TryUpdateModelAsync(search.Pager);
            var query = SearchSql(search);
            var pageList = new commons.util.PageList<WpAccountDto>();
            int count = 0;
            pageList.List = query.Select<WpAccountDto>().ToPageList(search.Pager.PageIndex, search.Pager.PageSize, ref count);
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
            return PartialView(new WpAccount());
        }
        public IActionResult DoAdd(WpAccount add)
        {
            var rv = new ReturnValue<string>();
            add.Id = Guid.NewGuid().ToString();
            var now = DateTime.Now;
            add.CreateTime = now;
            add.UpdateTime = now;
            var num = Db.Insertable<WpAccount>(add).ExecuteCommand();
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
            var model = Db.Queryable<WpAccount>().Where(t => t.Id == Id).First();
            return PartialView(model);
        }
        public IActionResult DoEdit(WpAccount edit)
        {
            var rv = new ReturnValue<string>();
            var model = Db.Queryable<WpAccount>().Where(t => t.Id == edit.Id).First();

            edit.UpdateTime = DateTime.Now;
            var num = Db.Updateable<WpAccount>(edit).ExecuteCommand();
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
            Db.Deleteable<WpAccount>().Where(it => it.Id == Id).ExecuteCommand();
            rv.True("删除完成");
            return Json(rv);
        }
        private ISugarQueryable<WpAccount> SearchSql(WpAccountSearch search)
        {
            var query = Db.Queryable<WpAccount>();
            if (!string.IsNullOrEmpty(search.Site))
            {
                query.Where(t => t.Site.Contains(search.Site));
            }

            return query;
        }
    }
}
