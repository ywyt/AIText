﻿using commons.util;

namespace AIText.Models.SiteKeyword
{
    public class SiteKeywordSearch
    {
        public System.String Keyword { get; set; }
        public PageModel Pager { get; set; } = new PageModel();
    }
}
