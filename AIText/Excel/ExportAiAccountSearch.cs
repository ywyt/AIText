using AIText.Controllers;
using commons.util;
using System;
using System.Collections.Generic;
using System.Text;

namespace model.excel
{
    public class ExportAiAccountSearch : List<ExportAiAccountDto>
    {
        public int? IsDesignImage { get; set; }
        public string SkuId { get; set; }
    }
    public class ExportAiAccountDto
    {
        public System.String Id { get; set; }

        public System.String Site { get; set; }

        public System.String ApiKey { get; set; }

        public bool IsEnable { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }
    }
}
