using AIText.Models.BusinessKeyword;
using commons.util;
using Entitys;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using model.excel;
using NPOI.SS.UserModel;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AIText.Controllers
{
    public class BusinessKeywordController : BaseController
    {
        private readonly SqlSugarClient Db;
        public BusinessKeywordController(SqlSugarClient _Db)
        {
            Db = _Db;
        }
        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> DoListAsync(BusinessKeywordSearch search)
        {
            await TryUpdateModelAsync(search.Pager);
            var query = SearchSql(search);
            var pageList = new commons.util.PageList<BusinessKeywordDto>();
            int count = 0;
            pageList.List = query.Select<BusinessKeywordDto>().ToPageList(search.Pager.PageIndex, search.Pager.PageSize, ref count);
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

            return PartialView(new BusinessKeyword());
        }
        public IActionResult DoAdd(BusinessKeyword add)
        {
            var rv = new ReturnValue<string>();
            add.Id = Guid.NewGuid().ToString();
            var now = DateTime.Now;
            add.CreateTime = now;
            add.UpdateTime = now;
            var num = Db.Insertable<BusinessKeyword>(add).ExecuteCommand();
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
            var model = Db.Queryable<BusinessKeyword>().Where(t => t.Id == Id).First();
            return PartialView(model);
        }
        public IActionResult DoEdit(BusinessKeyword edit)
        {
            var rv = new ReturnValue<string>();
            var model = Db.Queryable<BusinessKeyword>().Where(t => t.Id == edit.Id).First();

            edit.UpdateTime = DateTime.Now;
            var num = Db.Updateable<BusinessKeyword>(edit).ExecuteCommand();
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
            Db.Deleteable<BusinessKeyword>().Where(it => it.Id == Id).ExecuteCommand();
            rv.True("删除完成");
            return Json(rv);
        }
        private ISugarQueryable<BusinessKeyword> SearchSql(BusinessKeywordSearch search)
        {
            var query = Db.Queryable<BusinessKeyword>();
            if (!string.IsNullOrEmpty(search.Keyword))
            {
                query.Where(t => t.Keyword.Contains(search.Keyword));
            }

            return query;
        }

        public IActionResult Import()
        {
            return View();
        }

        [HttpPost]
        // accept request bodies up to 280,000,000 bytes.
        [RequestSizeLimit(280_000_000)]
        public async Task<IActionResult> ToImportAsync(IFormFile excelfile)
        {
            if (excelfile == null)
                return PartialView(new List<commons.import.ImportBusinessKeywordDto>());
            var list = await ExcelHelper<commons.import.ImportBusinessKeywordDto>.ImportFromExcel(excelfile);

            #region 验证

            List<commons.import.ImportBusinessKeywordDto> errlist = new List<commons.import.ImportBusinessKeywordDto>();

            int loopi = 0;
            foreach (var item in list)
            {
                loopi++;


                item.Idx = loopi;

            }

            #endregion

            list = list.OrderBy(o => string.IsNullOrEmpty(o.ErrMsg)).ThenBy(o => o.Idx).ToList();

            return PartialView(list);
        }

        [HttpPost]
        // ASP.NET Core在FormReader内强制将键/值的长度限制限制为2048，改变这个大小以便传入更大的表单内容
        [RequestFormLimits(ValueCountLimit = ushort.MaxValue)]
        public IActionResult DoImport1(List<commons.import.ImportBusinessKeywordDto> imports)
        {
            ReturnValue<string> rv = new ReturnValue<string>();

            if ((imports?.Count > 0) == false)
            {
                rv.False("参数错误");
                return Json(rv);
            }
            rv.status = true;
            List<string> errMsg = new List<string>();
            List<BusinessKeyword> insertList = new List<BusinessKeyword>();
            List<BusinessKeyword> updateList = new List<BusinessKeyword>();
            foreach (var item in imports)
            {
                if (string.IsNullOrEmpty(item.Keyword))
                {
                    rv.status = false;
                    errMsg.Add($"{item.Idx}关键词不能为空");
                    continue;
                }
                if (string.IsNullOrEmpty(item.Id))
                {
                    insertList.Add(new BusinessKeyword
                    {
                        Id = Guid.NewGuid().ToString(),
                        Keyword = item.Keyword,
                        Intent = item.Intent,
                        Volume = item.Volume,
                        PotentialTraffic = item.PotentialTraffic,
                        KeywordDifficulty = item.KeywordDifficulty,
                        CPC = item.CPC,
                        CreateTime = DateTime.Now,
                        UpdateTime = null
                    });
                }
                else
                {
                    var model = Db.Queryable<BusinessKeyword>().Where(t => t.Id == item.Id).First();
                    if (model == null)
                    {
                        rv.status = false;
                        errMsg.Add($"{item.Idx}不存在");
                        continue;
                    }
                    model.Keyword = item.Keyword;
                    model.Intent = item.Intent;
                    model.Volume = item.Volume;
                    model.PotentialTraffic = item.PotentialTraffic;
                    model.KeywordDifficulty = item.KeywordDifficulty;
                    model.CPC = item.CPC;

                    model.UpdateTime = DateTime.Now;
                    updateList.Add(model);
                }
            }
            int insertNum = 0, updateNum = 0;
            if (insertList.Count > 0)
            {
                insertNum = Db.Insertable(insertList).ExecuteCommand();
            }
            if (updateList.Count > 0)
            {
                insertNum = Db.Updateable(updateList).ExecuteCommand();
            }
            if (errMsg.Count > 0)
            {
                rv.errorsimple = string.Join("\r\n", errMsg);
                rv.errorsimple += $"\r\n\r\n导入完成,新增{insertNum}条，修改{updateNum}条";
            }
            else
            {
                rv.True($"导入完成,新增{insertNum}条，修改{updateNum}条");
            }

            return Json(rv);
        }

        public IActionResult DoExport(BusinessKeywordSearch search)
        {
            var list = SearchSql(search).Select<ExportBusinessKeywordDto>().ToList();
            //foreach (var item in list)
            //{
            //    if (!string.IsNullOrEmpty(item.ImageUrl))
            //    {
            //        string pattern = @"^\/.*\.jpg$";
            //        if (Regex.IsMatch(item.ImageUrl, pattern))
            //        {
            //            item.ImageUrl = erpimg + item.ImageUrl;
            //        }
            //    }
            //    if (!string.IsNullOrEmpty(item.Properties))
            //    {
            //        string pattern = @"http[^ ]+\.jpg";
            //        Match matches = Regex.Match(item.Properties, pattern);
            //        if (matches.Success)
            //        {
            //            item.PropertiesUrl = matches.Value.ToString();
            //        }
            //    }
            //}
            if (!(list?.Count > 0))
            {
                return Ok("没有数据导出，请返回修改查询条件");
            }
            //var exportHead = commons.import.ExpportPreProduce.GetHeadColums();
            var rvList = new commons.import.ExportBusinessKeyword().SetExport(list);

            // 转换为XSSFWorkbook
            var xss = ExcelHelper<commons.import.ExportBusinessKeywordTemp>.CreateExportXss(rvList, null, null);

            #region 重新设置单元格大小

            // 默认只有一个sheet
            var sheet = xss.GetSheetAt(0);

            // 过窄的改宽
            if (sheet.GetColumnWidth(5) < 25 * 256)
            {
                sheet.SetColumnWidth(5, 25 * 256);
            }

            #endregion

            byte[] bytes = new byte[0];
            using (MemoryStream ms = new MemoryStream())
            {
                xss.Write(ms, true);
                xss.Close();
                ms.Flush();
                ms.Position = 0;
                bytes = ms.ToArray();
            }
            string filename = "AI账号配置-" + System.DateTime.Now.ToString("yyyyMMddHHmm") + ".xlsx";

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
        }
    }
}
