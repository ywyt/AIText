using AIText.Controllers;
using commons.util;
using System;
using System.Collections.Generic;
using System.Text;

namespace model.excel
{
    public class ExportSiteKeywordSearch : List<ExportSiteKeywordDto>
    {
        public int? IsDesignImage { get; set; }
        public string SkuId { get; set; }
    }
    public class ExportSiteKeywordDto
    {
        public string Id { get; set; }
        public string Keyword { get; set; }
        public int Position { get; set; }
        public int PreviousPosition { get; set; }
        public int SearchVolume { get; set; }
        public int KeywordDifficulty { get; set; }
        public double CPC { get; set; }
        public string URL { get; set; }
        public int Traffic { get; set; }
        public decimal TrafficPercent { get; set; }
        public decimal TrafficCost { get; set; }
        public DateTime CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }
    }
}
