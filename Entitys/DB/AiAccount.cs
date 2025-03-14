﻿using SqlSugar;
using System;

namespace Entitys
{
    [SugarTable("ai_account")]
    public class AiAccount
    {
        /// <summary>
        /// 主键
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public System.String Id { get; set; }

        public System.String Site { get; set; }

        public System.String ApiKey { get; set; }

        public bool IsEnable {  get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }
    }
}
