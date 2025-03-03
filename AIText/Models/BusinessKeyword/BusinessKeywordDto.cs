using System;

namespace AIText.Models.BusinessKeyword
{
    public class BusinessKeywordDto
    {
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


        public DateTime CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }
    }
}
