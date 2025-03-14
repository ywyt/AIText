﻿using model.excel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace commons.import
{
    public class ExportAiAccount
    {
        public List<ExportAiAccountTemp> SetExport(IList<ExportAiAccountDto> expList)
        {
            var rvList = new List<ExportAiAccountTemp>();
            string pattern = @"http[^ ]+\.jpg";
            foreach (ExportAiAccountDto info in expList)
            {
                var temp = new ExportAiAccountTemp
                {
                    主键 = info.Id,
                    站点 = info.Site,
                    密钥 = info.ApiKey,
                    是否启用 = info.IsEnable ? "是" : "否",
                    创建时间 = info.CreateTime,
                    更新时间 = info.UpdateTime
                };

                rvList.Add(temp);
            }
            return rvList;
        }
    }

    public class ExportAiAccountTemp
    {
        public string 主键 { get; set; }
        public string 站点 { get; set; }
        public string 密钥 { get; set; }
        public string 是否启用 { get; set; }
        public DateTime 创建时间 { get; set; }
        public DateTime? 更新时间 { get; set; }
    }

}
