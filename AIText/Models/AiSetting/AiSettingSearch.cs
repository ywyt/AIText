using commons.util;
using System;

namespace AIText.Models.AiSetting
{
    public class AiSettingSearch
    {
        public string Site { get; set; }
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
