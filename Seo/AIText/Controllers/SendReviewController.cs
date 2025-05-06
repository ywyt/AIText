using AIText.Models.SendReview;
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
using Org.BouncyCastle.Bcpg.OpenPgp;
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
    public class SendReviewController : BaseController
    {
        static Random ReviewRandom = new Random();
        private readonly SqlSugarClient Db;
        private readonly IWebHostEnvironment _env;
        public SendReviewController(SqlSugarClient _Db, IWebHostEnvironment env)
        {
            Db = _Db;
            _env = env;
        }
        public IActionResult Index()
        {
            var siteAccount = Db.Queryable<SiteAccount>().Where(o => o.IsEnable == true).ToList();
            ViewData["Site"] = siteAccount;

            var search = new SendReviewSearch { BeginTime = DateTime.Now.Date };
            return View(search);
        }
        public async Task<IActionResult> DoListAsync(SendReviewSearch search)
        {
            await TryUpdateModelAsync(search.Pager);
            var query = SearchSql(search);
            var pageList = new commons.util.PageList<SendReviewDto>();
            int count = 0;
            pageList.List = query.Select<SendReviewDto>().ToPageList(search.Pager.PageIndex, search.Pager.PageSize, ref count);
            pageList.PagerModel = new commons.util.PageModel()
            {
                PageSize = search.Pager.PageSize,
                PageIndex = search.Pager.PageIndex,
                Count = count
            };
            return PartialView(pageList);
        }

        public IActionResult ViewAiAll()
        {
            var siteAccount = Db.Queryable<SiteAccount>().Where(o => o.IsEnable == true).ToList();
            ViewData["Site"] = siteAccount;

            var search = new SendReviewSearch { BeginTime = DateTime.Now.Date };
            return View(search);
        }

        public async Task<IActionResult> DoAiListAsync(SendReviewSearch search)
        {
            await TryUpdateModelAsync(search.Pager);
            var query = Db.Queryable<AiReview>();
            if (search.BeginTime.HasValue)
            {
                query.Where(t => t.CreateTime >= search.BeginTime);
            }
            if (search.EndTime.HasValue)
            {
                query.Where(t => t.CreateTime <= search.EndTime);
            }
            if (!string.IsNullOrEmpty(search.SyncSiteId))
            {
                query.Where(t => SqlFunc.Subqueryable<SiteProduct>().Where(p => p.Id == t.SiteProductId && p.SiteId == search.SyncSiteId ).Any());
            }
            var pageList = new commons.util.PageList<AiReviewDto>();
            int count = 0;
            pageList.List = query.Select<AiReviewDto>().ToPageList(search.Pager.PageIndex, search.Pager.PageSize, ref count);
            pageList.PagerModel = new commons.util.PageModel()
            {
                PageSize = search.Pager.PageSize,
                PageIndex = search.Pager.PageIndex,
                Count = count
            };
            if (pageList.List.Count > 0)
            {
                var pIds = pageList.List.Select(t => t.SiteProductId).ToList();
                var siteProducts = Db.Queryable<SiteProduct>().Where(t => pIds.Contains(t.Id)).ToList();
                foreach (var item in pageList.List)
                {
                    var siteProduct = siteProducts.Where(t => t.Id == item.SiteProductId).FirstOrDefault();
                    if (siteProduct != null)
                    {
                        item.ProductName = siteProduct.name;
                        item.Permalink = siteProduct.Permalink;
                        item.Site = siteProduct.Site;
                    }
                }
            }
            return PartialView(pageList);
        }

        public IActionResult ViewAiReview(int Id)
        {
            var list = Db.Queryable<AiReview>().Where(t => t.SiteProductId == Id).ToList();
            if (list.Count > 0) 
            {
                var siteProduct = Db.Queryable<SiteProduct>().Where(t => t.Id == Id).First();
                ViewData["SiteProduct"] = siteProduct;
            }
            return View(list);
        }

        public IActionResult ViewSiteReview(int Id)
        {
            var list = Db.Queryable<SiteReview>().Where(t => t.AiReviewId == Id).ToList();
            if (list.Count > 0)
            {
                var siteProduct = Db.Queryable<SiteProduct>().Where(t => t.Id == list[0].SiteProductId).First();
                ViewData["SiteProduct"] = siteProduct;
            }
            return View(list);
        }

        public IActionResult Add()
        {
            var setting = Db.Queryable<SiteAccount>().Where(o => o.IsEnable == true && o.StartDate <= DateTime.Now).ToList();
            ViewData["Sites"] = setting;
            return PartialView(new SendReview());
        }

        /// <summary>
        /// 创建记录
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public async Task<IActionResult> DoAdd(string Id)
        {
            var rv = new ReturnValue<SendReview>();
            var setting = Db.Queryable<SiteAccount>().Where(o => o.Id == Id).First();
            if (setting != null)
            {
                //rv = await InvokeApi.CreateReview(Db, setting);
            }
            return Json(rv);
        }

        public async Task<IActionResult> DoDrawReview(int Id)
        {
            var rv = new ReturnValue<string>();
            var sendReview = Db.Queryable<SendReview>().Where(o => o.Id == Id).First();
            if (sendReview.IsSync == true)
            {
                rv.False("评论已经同步");
                return Json(rv);
            }
            if (sendReview.SiteReviewId > 0)
            {
                rv.False("已有评论内容");
                return Json(rv);
            }
            var syncResult = await InvokeApi.DrawAIReview(Db, sendReview);
            return Json(syncResult);
        }

        public async Task<IActionResult> DoSync(int Id)
        {
            var rv = new ReturnValue<string>();
            var sendReview = Db.Queryable<SendReview>().Where(o => o.Id == Id).First();
            if (sendReview.IsSync == true)
            {
                rv.False("评论已经同步");
                return Json(rv);
            }
            if (string.IsNullOrEmpty(sendReview.Content))
            {
                rv.False("评论内容未生成");
                return Json(rv);
            }
            var syncResult = await InvokeApi.DoSyncReview(Db, sendReview);
            return Json(syncResult);
        }

        public IActionResult Detail(int Id)
        {
            var model = Db.Queryable<SendReview>().Where(t => t.Id == Id).First();
            if (model != null)
            {
                var siteProduct = Db.Queryable<SiteProduct>().Where(t => t.Id == model.SiteProductId).First();
                ViewData["SiteProduct"] = siteProduct;
            }
            return View(model);
        }

        public IActionResult DoDelete(int Id)
        {
            var rv = new ReturnValue<string>();
            Db.Deleteable<SendReview>().Where(it => it.Id == Id).ExecuteCommand();
            rv.True("删除完成");
            return Json(rv);
        }

        public async Task<IActionResult> DoReviewDelete(int Id)
        {
            var rv = new ReturnValue<string>();
            var review = await Db.Queryable<SendReview>().Where(it => it.Id == Id).FirstAsync();
            if (review != null)
            {
                var site = await Db.Queryable<SiteAccount>().Where(o => o.Id == review.SyncSiteId).FirstAsync();
                rv = await WordpressApi.WcReviewDelete(site.Site, site.WcKey, site.WcSecret, review.ReviewId);
                bool updateDelete = false;
                if (rv.errorsimple?.StartsWith("410") == true)
                {
                    rv.False("已删除过了");
                    updateDelete = true;
                }
                else if (rv.status)
                {
                    updateDelete = true;
                }

                if (updateDelete)
                {
                    review.IsSyncDelete = true;
                    await Db.Updateable(review).ExecuteCommandAsync();
                }
            }
            else
            {
                rv.False("不存在");
            }
            return Json(rv);
        }

        private ISugarQueryable<SendReview> SearchSql(SendReviewSearch search)
        {
            var query = Db.Queryable<SendReview>();
            if (!string.IsNullOrEmpty(search.SyncSiteId))
            {
                query.Where(t => t.SyncSiteId == search.SyncSiteId);
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
