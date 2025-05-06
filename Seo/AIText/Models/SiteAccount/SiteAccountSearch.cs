using commons.util;
using Entitys;

namespace AIText.Models.SiteAccount
{
    public class SiteAccountSearch
    {
        public System.String Site { get; set; }
        public SiteType? SiteType { get; set; }
        public PageModel Pager { get; set; } = new PageModel();
    }
}
