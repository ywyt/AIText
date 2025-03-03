using commons.util;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace commons.import
{
    public class ImportSiteKeyword
    {
        
    }
    public class ImportSiteKeywordDto
    {

        [Description("ID")]
        public string Id { get; set; }

        public string Keyword { get; set; }

        public int Position { get; set; }
        [Description("Previous position")]
        public int PreviousPosition { get; set; }
        [Description("Search Volume")]
        public int SearchVolume { get; set; }
        [Description("Keyword Difficulty")]
        public int KeywordDifficulty { get; set; }

        public double CPC { get; set; }

        public string URL { get; set; }

        public int Traffic { get; set; }
        [Description("Traffic (%)")]
        public decimal TrafficPercent { get; set; }
        [Description("Traffic Cost")]
        public decimal TrafficCost { get; set; }

        #region 做页面输出用，跟导入无关
        public string ErrMsg { get; set; }
        public int Idx { get; set; }
        #endregion
    }
}
