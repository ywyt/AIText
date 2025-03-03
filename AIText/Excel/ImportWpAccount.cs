using commons.util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace commons.import
{
    public class ImportWpAccount
    {
        
    }
    public class ImportWpAccountDto
    {
        public string 主键 { get; set; }
        public string 站点 { get; set; }
        public string 用户名 { get; set; }
        public string 密码 { get; set; }

        public bool 是否启用 { get; set; }

        #region 做页面输出用，跟导入无关
        public string ErrMsg { get; set; }
        public int Idx { get; set; }
        #endregion
    }
}
