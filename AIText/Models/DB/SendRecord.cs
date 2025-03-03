using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIText
{
    [SugarTable("send_record")]
    public class SendRecord
    {
        /// <summary>
        /// 主键
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public int Id { get; set; }

        public string SettingId { get; set; }
        public string Prompt { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public bool IsSync { get; set; }

        public string SyncSite { get; set; }

        public DateTime? SyncTime { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }
    }
}
