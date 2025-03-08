using commons.util;

namespace AIText.Models.PromptTemplate
{
    public class PromptTemplateSearch
    {
        public System.String Keyword { get; set; }
        public PageModel Pager { get; set; } = new PageModel();
    }
}
