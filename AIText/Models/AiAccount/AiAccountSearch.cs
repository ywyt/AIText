using commons.util;

namespace AIText.Models.AiAccount
{
    public class AiAccountSearch
    {
        public System.String Site { get; set; }
        public PageModel Pager { get; set; } = new PageModel();
    }
}
