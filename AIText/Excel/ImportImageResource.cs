using commons.util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace commons.import
{
    public class ImportImageResource
    {
        
    }
    public class ImportImageResourceDto
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
        [Description("图片路径")]
        public string ImagePath { get; set; }

        #region 做页面输出用，跟导入无关
        public string ErrMsg { get; set; }
        public int Idx { get; set; }
        #endregion
    }
}
