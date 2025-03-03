using AIText.Controllers;
using commons.util;
using System;
using System.Collections.Generic;
using System.Text;

namespace model.excel
{
    public class ExportWpAccountSearch : List<ExportWpAccountDto>
    {
        public int? IsDesignImage { get; set; }
        public string SkuId { get; set; }
    }
    public class ExportWpAccountDto
    {
        public string Id { get; set; }
        public string Site { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string AccessKey { get; set; }
        public bool IsEnable { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
    }
}
