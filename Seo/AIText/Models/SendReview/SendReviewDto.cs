using System;

namespace AIText.Models.SendReview
{
    public class SendReviewDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int ReviewId { get; set; }

        public int SiteReviewId { get; set; }
        public DateTime? AiTime { get; set; }

        /// <summary>
        /// 用户名
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 评分
        /// </summary>
        public int Rating { get; set; }

        public bool IsSync { get; set; }

        public bool IsSyncDelete { get; set; }

        public string SyncSiteId { get; set; }

        public string SyncSite { get; set; }

        public DateTime? SyncTime { get; set; }

        public string SyncErrMsg { get; set; }

        public DateTime CreateTime { get; set; }
        public DateTime? date_created_gmt { get; set; }
    }
}
