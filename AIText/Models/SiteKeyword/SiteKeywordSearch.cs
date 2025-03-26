using commons.util;

namespace AIText.Models.SiteKeyword
{
    public class SiteKeywordSearch
    {
        public string Id { get; set; }
        public string Alias { get; set; }
        public string Keyword { get; set; }
        public int? UseCount { get; set; }
        public PageModel Pager { get; set; } = new PageModel();
    }
}
