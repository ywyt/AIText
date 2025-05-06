using AIText;
using AIText.Models.SiteAccount;
using AIText.Models.SiteProduct;
using Entitys;
using Entitys.WP;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NPOI.SS.Formula.Functions;
using SqlSugar;
using SqlSugar.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Work;

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
            if (add.CountPerDay > 0)
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
            if (edit.CountPerDay > 0 && model.CountPerDay != edit.CountPerDay)
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

            model.WcKey = edit.WcKey;
            model.WcSecret = edit.WcSecret;
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

        public async Task<IActionResult> TestProducts(string Id)
        {
            ReturnValue<string> rv = new ReturnValue<string>();
            var model = Db.Queryable<SiteAccount>().Where(t => t.Id == Id).First();
            if (model != null) 
            {
                rv.status = await GetProducts(model);
            }
            return Json(rv);
        }

        public async Task<IActionResult> TestReviews(int Id)
        {
            ReturnValue<string> rv = new ReturnValue<string>();
            var product = Db.Queryable<SiteProduct>().Where(t => t.Id == Id).First();
            if (product != null)
            {
                var site = Db.Queryable<SiteAccount>().Where(t => t.Id == product.SiteId).First();
                rv.status = await GetReviews(site, product.ProductId);
            }
            return Json(rv);
        }

        public async Task<IActionResult> TestAIReviews(int Id)
        {
            ReturnValue<string> rv = new ReturnValue<string>();
            var product = Db.Queryable<SiteProduct>().Where(t => t.Id == Id).First();
            if (product != null)
            {
                rv = await InvokeApi.DoAIReview(Db, product);
            }
            return Json(rv);
        }

        private async Task<bool> GetProducts(SiteAccount model, int page = 1)
        {
            int pageSize = 10;
            var rv = await WordpressApi.WcProducts(model.Site, model.WcKey, model.WcSecret, page, pageSize);
            if (rv.status)
            {
                List<WooCommerceProduct> datas = new List<WooCommerceProduct>();
                try
                {
                    datas = JsonConvert.DeserializeObject<List<WooCommerceProduct>>(rv.value);

                }
                catch (Exception ex)
                {
                    rv.False("数据解析出错");
                    return false;
                }
                if (datas != null && datas.Count > 0)
                {
                    List<SiteProduct> list = new List<SiteProduct>();
                    foreach (var item in datas)
                    {
                        // 已有记录
                        if (Db.Queryable<SiteProduct>().Any(o => o.ProductId == item.id))
                            continue;
                        var product = new SiteProduct()
                        {
                            SiteId = model.Id,
                            Site = model.Site,
                            ProductId = item.id,
                            Permalink = item.permalink?.ToString(),
                            date_created_gmt = item.date_created_gmt,
                            CreateTime = DateTime.Now,
                            ReviewsCount = 0,
                            status = item.status,
                            name = item.name
                        };
                        list.Add(product);
                    }
                    if (list.Count > 0)
                    {
                        // 批量插入
                        await Db.Insertable(list).ExecuteCommandAsync();
                    }
                    else
                    {
                        return true;
                    }

                    // 当前条数不足，说明已经到头了
                    if (datas.Count < pageSize)
                        return true;
                    else
                        return await GetProducts(model, page + 1);
                }
                else
                    return true;
            }
            else
                return false;
        }

        private async Task<bool> GetReviews(SiteAccount model, int productId)
        {
            int pageSize = 10;
            var rv = await WordpressApi.WcProductReviewsTotal(model.Site, model.WcKey, model.WcSecret, productId);
            if (rv.status)
            {
               await Db.Updateable<SiteProduct>()
                    .SetColumns(it => it.ReviewsCount == rv.value)
                    .Where(it => it.SiteId == model.Id && it.ProductId == productId)
                    .ExecuteCommandAsync();
                return true;
            }
            else
                return false;
        }

        public IActionResult Products(string Id, int? ProductId)
        {
            var model = Db.Queryable<SiteAccount>().Where(t => t.Id == Id).First();
            if (model != null)
            {
                ViewData["SiteName"] = model.Site;
            }
            var search = new SiteProductSearch() { SiteId = Id, ProductId = ProductId };
            return View(search);
        }

        public async Task<IActionResult> DoProducts(SiteProductSearch search)
        {
            await TryUpdateModelAsync(search.Pager);
            var query = Db.Queryable<SiteProduct>().Where(o => o.SiteId == search.SiteId);
            if (search.ProductId > 0) 
            {
                query.Where(o => o.ProductId == search.ProductId);
            }
            query.OrderByDescending(o => o.date_created_gmt);
            var pageList = new commons.util.PageList<SiteProductDto>();
            int count = 0;
            pageList.List = query.Select<SiteProductDto>().ToPageList(search.Pager.PageIndex, search.Pager.PageSize, ref count);
            pageList.PagerModel = new commons.util.PageModel()
            {
                PageSize = search.Pager.PageSize,
                PageIndex = search.Pager.PageIndex,
                Count = count
            };
            return PartialView(pageList);
        }

        #region private
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
            query.OrderBy(t => t.CreateTime, OrderByType.Desc);
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
        #endregion
    }
}
