using model.excel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace commons.import
{
    public class ExportBusinessKeyword
    {
        public List<ExportBusinessKeywordTemp> SetExport(IList<ExportBusinessKeywordDto> expList)
        {
            var rvList = new List<ExportBusinessKeywordTemp>();
            string pattern = @"http[^ ]+\.jpg";
            foreach (ExportBusinessKeywordDto info in expList)
            {
                var temp = new ExportBusinessKeywordTemp
                {
                    Id = info.Id,
                    Keyword = info.Keyword,
                    Intent = info.Intent,
                    Volume = info.Volume,
                    PotentialTraffic = info.PotentialTraffic,
                    KeywordDifficulty = info.KeywordDifficulty,
                    CPC = info.CPC,
                    创建时间 = info.CreateTime,
                    更新时间 = info.UpdateTime
                };

                rvList.Add(temp);
            }
            return rvList;
        }
    }

    public class ExportBusinessKeywordTemp
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

        public DateTime 创建时间 { get; set; }
        public DateTime? 更新时间 { get; set; }
    }

}
