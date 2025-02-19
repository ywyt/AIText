using commons.util;
using System;

namespace AIText.Models.AiSetting
{
    public class AiSettingSearch
    {

        public string Prompt { get; set; }
        public string AiSite { get; set; }
        public string WpSite { get; set; }
        public bool? IsEnable { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? EndTime { get; set; }
        public PageModel Pager { get; set; } = new PageModel();
    }
}
