using model.excel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace commons.import
{
    public class ExportWpAccount
    {
        public List<ExportWpAccountTemp> SetExport(IList<ExportWpAccountDto> expList)
        {
            var rvList = new List<ExportWpAccountTemp>();
            string pattern = @"http[^ ]+\.jpg";
            foreach (ExportWpAccountDto info in expList)
            {
                var temp = new ExportWpAccountTemp
                {
                    主键 = info.Id,
                    站点 = info.Site,
                    用户名 = info.Username,
                    密码 = info.Password,
                    是否启用 = info.IsEnable ? "是" : "否",
                    创建时间 = info.CreateTime,
                    更新时间 = info.UpdateTime,

                };

                rvList.Add(temp);
            }
            return rvList;
        }
    }

    public class ExportWpAccountTemp
    {
        public string 主键 { get; set; }
        public string 站点 { get; set; }
        public string 用户名 { get; set; }
        public string 密码 { get; set; }
        //public string JWT的Token { get; set; }
        public string 是否启用 { get; set; }
        public DateTime 创建时间 { get; set; }
        public DateTime? 更新时间 { get; set; }

    }

}
