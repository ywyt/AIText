using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entitys
{
    /// <summary>
    /// 评论记录
    /// </summary>
    [SugarTable("send_review")]
    public class SendReview
    {
        /// <summary>
        /// 主键
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public int Id { get; set; }
        public int SiteProductId { get; set; }
        public int ProductId { get; set; }
        public int ReviewId { get; set; }
        public string Content { get; set; }
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
        /// <summary>
        /// 站点的评论是否被删除
        /// </summary>
        public bool IsSyncDelete { get; set; }

        public string SyncSiteId { get; set; }

        public string SyncSite { get; set; }

        public DateTime? SyncTime { get; set; }

        public string SyncErrMsg { get; set; }

        public DateTime CreateTime { get; set; }
        public DateTime? date_created_gmt { get; set; }
    }
}
