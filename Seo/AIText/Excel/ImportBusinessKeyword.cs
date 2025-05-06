using commons.util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace commons.import
{
    public class ImportBusinessKeyword
    {
        
    }
    public class ImportBusinessKeywordDto
    {

        [Description("ID")]
        public string Id { get; set; }

        public string Keyword { get; set; }

        public string Intent { get; set; }
        [Description("Volume")]
        public int Volume { get; set; }
        [Description("Potential Traffic")]
        public int PotentialTraffic { get; set; }
        [Description("Keyword Difficulty")]
        public int KeywordDifficulty { get; set; }
        [Description("CPC (USD)")]
        public double CPC { get; set; }

        #region 做页面输出用，跟导入无关
        public string ErrMsg { get; set; }
        public int Idx { get; set; }
        #endregion
    }
}
