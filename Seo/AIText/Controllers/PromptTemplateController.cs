using AIText.Models.PromptTemplate;
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
    public class PromptTemplateController : BaseController
    {
        public const string KEYWORD_PLACEHOLDER = "{keyword}";
        private readonly SqlSugarClient Db;
        public PromptTemplateController(SqlSugarClient _Db)
        {
            Db = _Db;
        }
        public IActionResult Index()
        {
            return View();
        }
        public async Task<IActionResult> DoListAsync(PromptTemplateSearch search)
        {
            await TryUpdateModelAsync(search.Pager);
            var query = SearchSql(search);
            var pageList = new commons.util.PageList<PromptTemplateDto>();
            int count = 0;
            pageList.List = query.Select<PromptTemplateDto>().ToPageList(search.Pager.PageIndex, search.Pager.PageSize, ref count);
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
            return PartialView(new PromptTemplate());
        }
        public IActionResult DoAdd(PromptTemplate add)
        {
            var rv = new ReturnValue<string>();
            if (string.IsNullOrEmpty(add.Prompt))
            {
                rv.False("指令不能为空");
                return Json(rv);
            }
            if (!add.Prompt.Contains(KEYWORD_PLACEHOLDER))
            {
                rv.False($"指令必须包含{KEYWORD_PLACEHOLDER}");
                return Json(rv);
            }

            add.Id = Guid.NewGuid().ToString();
            var now = DateTime.Now;
            add.CreateTime = now;
            add.UpdateTime = now;
            var num = Db.Insertable<PromptTemplate>(add).ExecuteCommand();
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
            var model = Db.Queryable<PromptTemplate>().Where(t => t.Id == Id).First();
            return PartialView(model);
        }
        public IActionResult DoEdit(PromptTemplate edit)
        {
            var rv = new ReturnValue<string>();
            if (string.IsNullOrEmpty(edit.Prompt))
            {
                rv.False("指令不能为空");
                return Json(rv);
            }
            if (!edit.Prompt.Contains(KEYWORD_PLACEHOLDER))
            {
                rv.False($"指令必须包含{KEYWORD_PLACEHOLDER}");
                return Json(rv);
            }

            edit.UpdateTime = DateTime.Now;
            var num = Db.Updateable<PromptTemplate>()
                        .SetColumns(o => o.Name == edit.Name)
                        .SetColumns(o => o.Prompt == edit.Prompt)
                        .SetColumns(o => o.IsEnable == edit.IsEnable)
                        .SetColumns(o => o.UpdateTime == edit.UpdateTime)
                        .ExecuteCommand();
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
            Db.Deleteable<PromptTemplate>().Where(it => it.Id == Id).ExecuteCommand();
            rv.True("删除完成");
            return Json(rv);
        }
        private ISugarQueryable<PromptTemplate> SearchSql(PromptTemplateSearch search)
        {
            var query = Db.Queryable<PromptTemplate>();
            if (!string.IsNullOrEmpty(search.Keyword))
            {
                query.Where(t => t.Prompt.Contains(search.Keyword));
            }

            return query;
        }
    }
}
