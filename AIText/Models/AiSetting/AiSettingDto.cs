using System;

namespace AIText.Models.AiSetting
{
    public class AiSettingDto
    {
        public string Id { get; set; }

        public string Prompt { get; set; }

        /// <summary>
        /// 每天创建的文章数
        /// </summary>
        public int CountPerDay { get; set; }

        public string AiSiteId { get; set; }
        /// <summary>
        /// AI平台
        /// </summary>
        public string AiSite { get; set; }

        public string WpSiteId { get; set; }
        /// <summary>
        /// 需要推送到的Wordpress站点
        /// </summary>
        public string WpSite { get; set; }

        public bool IsEnable { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }
    }
}
