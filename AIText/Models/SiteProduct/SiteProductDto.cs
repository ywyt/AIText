using System;

namespace AIText.Models.SiteProduct
{
    public class SiteProductDto
    {

        public int Id { get; set; }
        public System.String SiteId { get; set; }
        /// <summary>
        /// 商品ID
        /// </summary>
        public int ProductId { get; set; }
        /// <summary>
        /// 商品链接
        /// </summary>
        public string Permalink { get; set; }
        /// <summary>
        /// 评论个数
        /// </summary>
        public int ReviewsCount { get; set; }
        /// <summary>
        /// 记录创建时间
        /// </summary>
        public DateTime? CreateTime { get; set; }
        /// <summary>
        /// WP上的商品创建时间
        /// </summary>
        public DateTime? date_created_gmt { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        public string status { get; set; }
        /// <summary>
        /// 商品名称
        /// </summary>
        public string name { get; set; }
    }
}
