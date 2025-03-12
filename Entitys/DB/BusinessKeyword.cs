using SqlSugar;
using System;

namespace Entitys
{
    [SugarTable("business_keyword")]
    public class BusinessKeyword
    {
        /// <summary>
        /// 主键
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public string Id { get; set; }
        /// <summary>
        /// 关键词
        /// </summary>
        public string Keyword { get; set; }
        /// <summary>
        /// 意图
        /// </summary>
        public string Intent { get; set; }
        /// <summary>
        /// 流量量级
        /// </summary>
        public int Volume { get; set; }
        /// <summary>
        /// 潜在流量
        /// </summary>
        public int PotentialTraffic { get; set; }
        /// <summary>
        /// 关键词难度
        /// </summary>
        public int KeywordDifficulty { get; set; }
        /// <summary>
        /// 每次点击成本USD
        /// </summary>
        public double CPC { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }
        /// <summary>
        /// 更新时间
        /// </summary>
        public DateTime? UpdateTime { get; set; }
    }
}
