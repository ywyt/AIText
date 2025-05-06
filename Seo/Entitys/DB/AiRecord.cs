using SqlSugar;
using System;

namespace Entitys
{
    [SugarTable("ai_record")]
    public class AiRecord
    {
        /// <summary>
        /// 主键
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public int Id { get; set; }

        public int SendRecordId { get; set; }

        public System.String Prompt { get; set; }

        public System.String Content { get; set; }

        public System.String ErrMsg { get; set; }

        public DateTime CreateTime { get; set; }
    }
}
