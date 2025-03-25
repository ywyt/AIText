using model.excel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace commons.import
{
    public class ExportImageResource
    {
        public List<ExportImageResourceTemp> SetExport(IList<ExportImageResourceDto> expList)
        {
            var rvList = new List<ExportImageResourceTemp>();
            foreach (ExportImageResourceDto info in expList)
            {
                var temp = new ExportImageResourceTemp
                {
                    Id = info.Id,
                    FileName = info.FileName,
                    Color = info.Color,
                    创建时间 = info.CreateTime,
                };

                rvList.Add(temp);
            }
            return rvList;
        }
    }

    public class ExportImageResourceTemp
    {
        [Description("ID")]
        public string Id { get; set; }

        /// <summary>
        /// 文件夹 => 款式
        /// </summary>
        [Description("款式")]
        public string Style { get; set; }

        /// <summary>
        /// 文件夹 => 按颜色分组
        /// </summary>
        [Description("颜色")]
        public string Color { get; set; }

        /// <summary>
        /// 文件名
        /// </summary>
        [Description("文件名")]
        public string FileName { get; set; }

        public DateTime 创建时间 { get; set; }
    }

}
