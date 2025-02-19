using commons.util;

namespace AIText.Models.SendRecord
{
    public class SendRecordSearch
    {
        public string Prompt { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public bool? IsSync { get; set; }

        public string SyncSite { get; set; }
        public PageModel Pager { get; set; } = new PageModel();
    }
}
