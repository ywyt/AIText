using SqlSugar;
using System;

namespace Entitys
{
    [SugarTable("ai_review")]
    public class AiReview
    {
        /// <summary>
        /// 主键
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public int Id { get; set; }

        public int SiteProductId { get; set; }

        public System.String Prompt { get; set; }

        public System.String Content { get; set; }

        public System.String ErrMsg { get; set; }

        public DateTime CreateTime { get; set; }
    }
}
