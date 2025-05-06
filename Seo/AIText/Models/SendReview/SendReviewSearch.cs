using commons.util;
using System;

namespace AIText.Models.SendReview
{
    public class SendReviewSearch
    {
        public string SyncSiteId { get; set; }

        public bool? IsSync { get; set; }

        public DateTime? BeginTime { get; set; }
        public DateTime? EndTime { get; set; }

        public string SyncSite { get; set; }
        public PageModel Pager { get; set; } = new PageModel();
    }
}
