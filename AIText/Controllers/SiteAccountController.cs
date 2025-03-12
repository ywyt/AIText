using AIText.Models.SiteAccount;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System.Threading.Tasks;
using System;
using AIText;
using Entitys;

namespace AIText.Controllers
{
    public class SiteAccountController : BaseController
    {
        private readonly SqlSugarClient Db;
        public SiteAccountController(SqlSugarClient _Db)
        {
            Db = _Db;
        }
        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> DoListAsync(SiteAccountSearch search)
        {
            await TryUpdateModelAsync(search.Pager);
            var query = SearchSql(search);
            var pageList = new commons.util.PageList<SiteAccountDto>();
            int count = 0;
            pageList.List = query.Select<SiteAccountDto>().ToPageList(search.Pager.PageIndex, search.Pager.PageSize, ref count);
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
            return PartialView(new SiteAccount());
        }
        public IActionResult DoAdd(SiteAccount add)
        {
            var rv = new ReturnValue<string>();
            add.Id = Guid.NewGuid().ToString();
            var now = DateTime.Now;
            add.CreateTime = now;
            add.UpdateTime = now;
            var num = Db.Insertable<SiteAccount>(add).ExecuteCommand();
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
            var model = Db.Queryable<SiteAccount>().Where(t => t.Id == Id).First();
            return PartialView(model);
        }
        public IActionResult DoEdit(SiteAccount edit)
        {
            var rv = new ReturnValue<string>();
            var model = Db.Queryable<SiteAccount>().Where(t => t.Id == edit.Id).First();
            model.Site = edit.Site;
            model.Username = edit.Username;
            model.Password = edit.Password;
            model.CountPerDay = edit.CountPerDay;
            model.IsEnable = edit.IsEnable;
            model.StartDate = edit.StartDate;
            model.UpdateTime = DateTime.Now;
            var num = Db.Updateable<SiteAccount>(model).ExecuteCommand();
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
            Db.Deleteable<SiteAccount>().Where(it => it.Id == Id).ExecuteCommand();
            rv.True("删除完成");
            return Json(rv);
        }
        private ISugarQueryable<SiteAccount> SearchSql(SiteAccountSearch search)
        {
            var query = Db.Queryable<SiteAccount>();
            if (!string.IsNullOrEmpty(search.Site))
            {
                query.Where(t => t.Site.Contains(search.Site));
            }
            if (search.SiteType.HasValue)
            {
                query.Where(t => t.SiteType == search.SiteType);
            }
            return query;
        }
    }
}
