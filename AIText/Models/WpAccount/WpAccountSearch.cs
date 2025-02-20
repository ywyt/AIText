using commons.util;

namespace AIText.Models.WpAccount
{
    public class WpAccountSearch
    {
        public System.String Site { get; set; }
        public PageModel Pager { get; set; } = new PageModel();
    }
}
