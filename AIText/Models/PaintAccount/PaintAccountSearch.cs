using commons.util;

namespace AIText.Models.PaintAccount
{
    public class PaintAccountSearch
    {
        public System.String Site { get; set; }
        public PageModel Pager { get; set; } = new PageModel();
    }
}
