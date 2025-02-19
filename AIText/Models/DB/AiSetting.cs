using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIText
{
    [SugarTable("ai_setting")]
    public class AiSetting
    {
        /// <summary>
        /// 主键
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public string Id { get; set; }

        public string Prompt { get; set; }

        public int CountPerDay { get; set; }

        public bool IsEnable { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }
    }
}
