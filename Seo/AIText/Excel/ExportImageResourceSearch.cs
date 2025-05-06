using AIText.Controllers;
using commons.util;
using System;
using System.Collections.Generic;
using System.Text;

namespace model.excel
{
    public class ExportImageResourceSearch : List<ExportImageResourceDto>
    {
        public int? IsDesignImage { get; set; }
        public string SkuId { get; set; }
    }
    public class ExportImageResourceDto
    {
        public string Id { get; set; }

        public string Style { get; set; }
        public string Color { get; set; }
        public string FileName { get; set; }

        public DateTime CreateTime { get; set; }
    }
}
