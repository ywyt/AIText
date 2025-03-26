using commons.util;
using System;

namespace AIText.Models.SendRecord
{
    public class SendRecordSearch
    {
        public string SyncSiteId { get; set; }

        public string TemplateId { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public bool? IsSync { get; set; }

        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }

        public string SyncSite { get; set; }
        public PageModel Pager { get; set; } = new PageModel();
    }
}
