using model.excel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

namespace commons.import
{
    public class ExportSiteKeyword
    {
        public List<ExportSiteKeywordTemp> SetExport(IList<ExportSiteKeywordDto> expList)
        {
            var rvList = new List<ExportSiteKeywordTemp>();
            string pattern = @"http[^ ]+\.jpg";
            foreach (ExportSiteKeywordDto info in expList)
            {
                var temp = new ExportSiteKeywordTemp
                {
                    Id = info.Id,
                    Keyword = info.Keyword,
                    Position = info.Position,
                    PreviousPosition = info.PreviousPosition,
                    SearchVolume = info.SearchVolume,
                    KeywordDifficulty = info.KeywordDifficulty,
                    CPC = info.CPC,
                    URL = info.URL,
                    Traffic = info.Traffic,
                    TrafficPercent = info.TrafficPercent,
                    TrafficCost = info.TrafficCost,
                    创建时间 = info.CreateTime,
                    更新时间 = info.UpdateTime
                };

                rvList.Add(temp);
            }
            return rvList;
        }
    }

    public class ExportSiteKeywordTemp
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

        public DateTime 创建时间 { get; set; }
        public DateTime? 更新时间 { get; set; }
    }

}
