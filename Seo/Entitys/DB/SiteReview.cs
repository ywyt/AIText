using SqlSugar;
using System;
namespace Entitys
{
    [SugarTable("site_review")]
    public class SiteReview
    {
        /// <summary>
        /// 主键
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public int Id { get; set; }

        public System.String SiteId { get; set; }
        /// <summary>
        /// AI记录ID
        /// </summary>
        public int AiReviewId { get; set; }
        /// <summary>
        /// 站点商品ID
        /// </summary>
        public int SiteProductId { get; set; }
        /// <summary>
        /// 商品ID
        /// </summary>
        public int ProductId { get; set; }
        /// <summary>
        /// 评论内容
        /// </summary>
        public string Content { get; set; }
        /// <summary>
        /// 是否被使用
        /// </summary>
        public bool IsUse { get; set; }
        /// <summary>
        /// 记录创建时间
        /// </summary>
        public DateTime? CreateTime { get; set; }
    }
}
