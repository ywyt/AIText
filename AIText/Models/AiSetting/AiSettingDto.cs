using System;

namespace AIText.Models.AiSetting
{
    public class AiSettingDto
    {
        public string Id { get; set; }
        /// <summary>
        /// 每天创建的文章数
        /// </summary>
        public int CountPerDay { get; set; }

        public string AiSiteId { get; set; }
        /// <summary>
        /// AI平台
        /// </summary>
        public string AiSite { get; set; }

        public string SiteId { get; set; }
        /// <summary>
        /// 需要推送到的站点
        /// </summary>
        public string Site { get; set; }

        public bool IsEnable { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }
    }
}
