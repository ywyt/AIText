using AIText.Controllers;
using commons.util;
using System;
using System.Collections.Generic;
using System.Text;

namespace model.excel
{
    public class ExportBusinessKeywordSearch : List<ExportBusinessKeywordDto>
    {
        public int? IsDesignImage { get; set; }
        public string SkuId { get; set; }
    }
    public class ExportBusinessKeywordDto
    {
        public string Id { get; set; }
        public string Keyword { get; set; }
        public string Intent { get; set; }
        public int Volume { get; set; }
        public int PotentialTraffic { get; set; }
        public int KeywordDifficulty { get; set; }
        public double CPC { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }
    }
}
