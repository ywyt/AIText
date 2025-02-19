
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace commons.util
{
    public class PageList<T>
    {

        public IList<T> List { get; set; }

        public PageModel PagerModel { get; set; } = new PageModel();
    }
}
