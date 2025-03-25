using commons.util;

namespace AIText.Models.ImageResource
{
    public class ImageResourceSearch
    {
        public string SiteId { get; set; }
        public string Style { get; set; }
        public string Color { get; set; }
        public PageModel Pager { get; set; } = new PageModel();
    }
}
