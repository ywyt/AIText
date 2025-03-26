using AIText.Models.ImageResource;
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
    public class ImageResourceController : BaseController
    {
        private readonly SqlSugarClient Db;
        public ImageResourceController(SqlSugarClient _Db)
        {
            Db = _Db;
        }
        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> DoListAsync(ImageResourceSearch search)
        {
            await TryUpdateModelAsync(search.Pager);
            var query = SearchSql(search);
            var pageList = new commons.util.PageList<ImageResourceDto>();
            int count = 0;
            pageList.List = query.Select<ImageResourceDto>().ToPageList(search.Pager.PageIndex, search.Pager.PageSize, ref count);
            pageList.PagerModel = new commons.util.PageModel()
            {
                PageSize = search.Pager.PageSize,
                PageIndex = search.Pager.PageIndex,
                Count = count
            };
            return PartialView(pageList);
        }

        public IActionResult DoDelete(string Id)
        {
            var rv = new ReturnValue<string>();
            Db.Deleteable<ImageResource>().Where(it => it.Id == Id).ExecuteCommand();
            rv.True("删除完成");
            return Json(rv);
        }
        private ISugarQueryable<ImageResource> SearchSql(ImageResourceSearch search)
        {
            var query = Db.Queryable<ImageResource>();
            if (!string.IsNullOrEmpty(search.Style))
            {
                query.Where(t => t.Style == search.Style);
            }
            if (!string.IsNullOrEmpty(search.Color))
            {
                query.Where(t => t.Color == search.Color);
            }
            if (search.UseCount.HasValue)
            {
                query.Where(t => t.UseCount >= search.UseCount);
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
                return PartialView(new List<commons.import.ImportImageResourceDto>());
            var list = await ExcelHelper<commons.import.ImportImageResourceDto>.ImportFromExcel(excelfile);

            #region 验证

            List<commons.import.ImportImageResourceDto> errlist = new List<commons.import.ImportImageResourceDto>();

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
        public IActionResult DoImport([FromBody]List<commons.import.ImportImageResourceDto> imports)
        {
            var rv = ImportList(imports);

            return Json(rv);
        }

        [HttpPost]
        // ASP.NET Core在FormReader内强制将键/值的长度限制限制为2048，改变这个大小以便传入更大的表单内容
        [RequestFormLimits(ValueCountLimit = ushort.MaxValue)]
        [RequestSizeLimit(10 * 1024 * 1024)] // 允许最大 10MB 请求大小
        public IActionResult DoImport1(List<commons.import.ImportImageResourceDto> imports)
        {
            var rv = ImportList(imports);

            return Json(rv);
        }

        private ReturnValue<string> ImportList(List<commons.import.ImportImageResourceDto> imports)
        {
            ReturnValue<string> rv = new ReturnValue<string>();

            if ((imports?.Count > 0) == false)
            {
                rv.False("参数错误");
                return rv;
            }
            rv.status = true;
            List<string> errMsg = new List<string>();
            List<ImageResource> insertList = new List<ImageResource>();
            List<ImageResource> updateList = new List<ImageResource>();
            foreach (var item in imports)
            {
                if (string.IsNullOrEmpty(item.Style))
                {
                    errMsg.Add($"{item.Idx}款式不能为空");
                    continue;
                }
                if (string.IsNullOrEmpty(item.Color))
                {
                    errMsg.Add($"{item.Idx}颜色不能为空");
                    continue;
                }
                if (string.IsNullOrEmpty(item.ImagePath))
                {
                    errMsg.Add($"{item.Idx}文图片路径不能为空");
                    continue;
                }

                if (string.IsNullOrEmpty(item.Id) && Db.Queryable<ImageResource>().Any(o => o.Style == item.Style && o.Color == item.Color && o.ImagePath == item.ImagePath))
                {
                    errMsg.Add($"{item.Idx} 已存在{item.Style}、{item.Color}的图片{item.ImagePath}");
                    continue;
                }
                else if (!string.IsNullOrEmpty(item.Id) && Db.Queryable<ImageResource>().Any(o => o.Style == item.Style && o.Color == item.Color && o.ImagePath == item.ImagePath && o.Id != item.Id))
                {
                    errMsg.Add($"{item.Idx} 已存在{item.Style}、{item.Color}的图片{item.ImagePath}");
                    continue;
                }
                if (string.IsNullOrEmpty(item.Id))
                {
                    insertList.Add(new ImageResource
                    {
                        Id = Guid.NewGuid().ToString(),
                        ImagePath = item.ImagePath,
                        Color = item.Color,
                        Style = item.Style,
                        CreateTime = DateTime.Now,
                    });
                }
                else
                {
                    var model = Db.Queryable<ImageResource>().Where(t => t.Id == item.Id).First();
                    if (model == null)
                    {
                        rv.status = false;
                        errMsg.Add($"{item.Idx}不存在");
                        continue;
                    }
                    model.ImagePath = item.ImagePath;
                    model.Color = item.Color;
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
                rv.errordetailed = string.Join("\r\n", errMsg);
                rv.errorsimple = $"导入完成,新增{insertNum}条，修改{updateNum}条";
            }
            else
            {
                rv.True($"导入完成,新增{insertNum}条，修改{updateNum}条");
            }
            return rv;
        }

        public IActionResult DoExport(ImageResourceSearch search)
        {
            var list = SearchSql(search).Select<ExportImageResourceDto>().ToList();
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
            var rvList = new commons.import.ExportImageResource().SetExport(list);

            // 转换为XSSFWorkbook
            var xss = ExcelHelper<commons.import.ExportImageResourceTemp>.CreateExportXss(rvList, null, null);

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
            string filename = "图片资源配置-" + System.DateTime.Now.ToString("yyyyMMddHHmm") + ".xlsx";

            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", filename);
        }
    }
}
