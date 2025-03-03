using commons.util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace commons.import
{
    public class ImportAiAccount
    {
        
    }
    public class ImportAiAccountDto
    {
        public string 主键 { get; set; }
        public string 站点 { get; set; }
        public string 密钥 { get; set; }
        public string 是否启用 { get; set; }
        //public DateTime 创建时间 { get; set; }
        //public DateTime? 更新时间 { get; set; }
        #region 做页面输出用，跟导入无关
        public string ErrMsg { get; set; }
        public int Idx { get; set; }
        #endregion
    }
}
