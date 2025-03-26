using AIText;
using AIText.Models.SiteAccount;
using Entitys;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using NPOI.SS.Formula.Functions;
using SqlSugar;
using System;
using System.Linq;
using System.Threading.Tasks;

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
            add.Hours = string.Join(",", GetRandHours(add.CountPerDay));
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

            // 每天多少篇文章改变时
            if (model.CountPerDay != edit.CountPerDay)
            {
                // 重新计算
                if (!string.IsNullOrEmpty(model.Hours))
                {
                    var old = model.Hours.Split(',').Select(int.Parse).ToArray();
                    var newHours = GetRandHours(edit.CountPerDay, old);
                    model.Hours = string.Join(",", newHours);
                }
                else
                {
                    model.Hours = string.Join(",", GetRandHours(edit.CountPerDay));
                }
            }

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

        /// <summary>
        /// 获取执行任务，随机的小时段
        /// </summary>
        /// <param name="num">每天几篇</param>
        /// <param name="old">旧的小时数组，尽量保持不变</param>
        /// <returns></returns>
        private static int[] GetRandHours(int num, int[] old = null)
        {
            int phase = 24 / num;
            Random random = new Random();
            int[] numbers = new int[num];

            for (int i = 0, idx = 0; i <= 24 && idx < num; i = i + phase, idx++)
            {
                if (i == 0 && old?.Length > num)
                {
                    var yu = 24 % num;
                    if (yu == 0)
                    {
                        i = random.Next(0, phase);
                    }
                    else
                    {
                        i = random.Next(0, yu + 1);
                    }
                }

                if (old?.Length > 0)
                {
                    var ov = old.Where(o => o >= i && o < i + phase).ToList();
                    if (ov.Count > 0)
                    {
                        var oo = ov.First();
                        numbers[idx] = oo;
                        old = old.Where(val => val != oo).ToArray();
                        continue;
                    }
                }
                numbers[idx] = random.Next(i, i + phase > 24 ? 24 : i + phase);
            }
            return numbers;
        }
    }
}
