using AIText.Models.PaintAccount;
using commons.util;
using Entitys;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using model.excel;
using NPOI.OpenXmlFormats.Dml.Diagram;
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
    public class PaintAccountController : BaseController
    {
        private readonly SqlSugarClient Db;
        public PaintAccountController(SqlSugarClient _Db)
        {
            Db = _Db;
        }
        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> DoListAsync(PaintAccountSearch search)
        {
            await TryUpdateModelAsync(search.Pager);
            var query = SearchSql(search);
            var pageList = new commons.util.PageList<PaintAccountDto>();
            int count = 0;
            pageList.List = query.Select<PaintAccountDto>().ToPageList(search.Pager.PageIndex, search.Pager.PageSize, ref count);
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

            return PartialView(new PaintAccount());
        }
        public IActionResult DoAdd(PaintAccount add)
        {
            var rv = new ReturnValue<string>();
            add.Id = Guid.NewGuid().ToString();
            var now = DateTime.Now;
            add.CreateTime = now;
            add.UpdateTime = now;
            var num = Db.Insertable<PaintAccount>(add).ExecuteCommand();
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
            var model = Db.Queryable<PaintAccount>().Where(t => t.Id == Id).First();
            return PartialView(model);
        }
        public IActionResult DoEdit(PaintAccount edit)
        {
            var rv = new ReturnValue<string>();
            var model = Db.Queryable<PaintAccount>().Where(t => t.Id == edit.Id).First();

            edit.UpdateTime = DateTime.Now;
            var num = Db.Updateable<PaintAccount>(edit).ExecuteCommand();
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
            Db.Deleteable<PaintAccount>().Where(it => it.Id == Id).ExecuteCommand();
            rv.True("删除完成");
            return Json(rv);
        }
        private ISugarQueryable<PaintAccount> SearchSql(PaintAccountSearch search)
        {
            var query = Db.Queryable<PaintAccount>();
            if (!string.IsNullOrEmpty(search.Site))
            {
                query.Where(t => t.Site.Contains(search.Site));
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
                return PartialView(new List<commons.import.ImportPaintAccountDto>());
            var list = await ExcelHelper<commons.import.ImportPaintAccountDto>.ImportFromExcel(excelfile);

            #region 验证

            List<commons.import.ImportPaintAccountDto> errlist = new List<commons.import.ImportPaintAccountDto>();

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
        public IActionResult DoImport1(List<commons.import.ImportPaintAccountDto> imports)
        {
            ReturnValue<string> rv = new ReturnValue<string>();
            rv.status = true;
            List<string> errMsg = new List<string>();
            List<PaintAccount> insertList = new List<PaintAccount>();
            List<PaintAccount> updateList = new List<PaintAccount>();
            foreach (var item in imports)
            {
                if (string.IsNullOrEmpty(item.AccessKey) && string.IsNullOrEmpty(item.SecretKey))
                {
                    rv.status = false;
                    errMsg.Add($"{item.Idx}密钥不能为空");
                    continue;
                }
                if (string.IsNullOrEmpty(item.主键))
                {
                    insertList.Add(new PaintAccount
                    {
                        Id = Guid.NewGuid().ToString(),
                        Site = item.站点,
                        AccessKey = item.AccessKey,
                        SecretKey = item.SecretKey,
                        IsEnable = item.是否启用 == "是" ? true : false,
                        CreateTime = DateTime.Now,
                        UpdateTime = null
                    });
                }
                else
                {
                    var model = Db.Queryable<PaintAccount>().Where(t => t.Id == item.主键).First();
                    if (model == null)
                    {
                        rv.status = false;
                        errMsg.Add($"{item.Idx}账号不存在");
                        continue;
                    }
                    model.AccessKey = item.AccessKey;
                    model.SecretKey = item.SecretKey;
                    model.IsEnable = item.是否启用 == "是" ? true : false;
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

        public IActionResult DoExport(PaintAccountSearch search)
        {
            var list = SearchSql(search).Select<ExportPaintAccountDto>().ToList();
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
            var rvList = new commons.import.ExportPaintAccount().SetExport(list);

            // 转换为XSSFWorkbook
            var xss = ExcelHelper<commons.import.ExportPaintAccountTemp>.CreateExportXss(rvList, null, null);

            #region 重新设置图片的单元格大小

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
