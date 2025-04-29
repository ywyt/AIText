using commons.util;
using Entitys;

namespace AIText.Models.SiteProduct
{
    public class SiteProductSearch
    {
        public System.String SiteId { get; set; }
        public int? ProductId { get; set; }
        public PageModel Pager { get; set; } = new PageModel();
    }
}
