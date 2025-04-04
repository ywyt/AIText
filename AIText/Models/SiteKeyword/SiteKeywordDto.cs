﻿using System;

namespace AIText.Models.SiteKeyword
{
    public class SiteKeywordDto
    {
        /// <summary>
        /// ID
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 关键词
        /// </summary>
        public string Keyword { get; set; }
        ///// <summary>
        ///// 站点ID
        ///// </summary>
        //public string SiteId { get; set; }
        /// <summary>
        /// 域名
        /// </summary>
        public string Alias { get; set; }
        /// <summary>
        /// 当前排名
        /// </summary>
        public int Position { get; set; }
        /// <summary>
        /// 之前排名
        /// </summary>
        public int PreviousPosition { get; set; }
        /// <summary>
        /// 搜索量
        /// </summary>
        public int SearchVolume { get; set; }
        /// <summary>
        /// 关键词难度
        /// </summary>
        public int KeywordDifficulty { get; set; }
        /// <summary>
        /// 每次点击成本USD
        /// </summary>
        public double CPC { get; set; }
        /// <summary>
        /// 网址
        /// </summary>
        public string URL { get; set; }
        /// <summary>
        /// 流量
        /// </summary>
        public int Traffic { get; set; }
        /// <summary>
        /// 流量占比
        /// </summary>
        public decimal TrafficPercent { get; set; }
        /// <summary>
        /// 流量成本
        /// </summary>
        public decimal TrafficCost { get; set; }
        /// <summary>
        /// 使用次数
        /// </summary>
        public int UseCount { get; set; }
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
