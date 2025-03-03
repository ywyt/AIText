using commons.util;

namespace AIText.Models.BusinessKeyword
{
    public class BusinessKeywordSearch
    {
        public System.String Keyword { get; set; }
        public PageModel Pager { get; set; } = new PageModel();
    }
}
