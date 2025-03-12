using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entitys
{
    [SugarTable("send_record")]
    public class SendRecord
    {
        /// <summary>
        /// 主键
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public int Id { get; set; }

        public string AiSiteId { get; set; }
        public string AiSite { get; set; }
        public string Link { get; set; }
        public string KeywordId { get; set; }
        public string Keyword { get; set; }
        public string TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string Prompt { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ErrMsg { get; set; }
        public DateTime? AiTime { get; set; }
        public string ImgUrl { get; set; }
        public string ImgPath { get; set; }
        public DateTime? ImgTime { get; set; }
        public string ImgErrMsg { get; set; }

        public bool IsSync { get; set; }

        public string SyncSiteId { get; set; }

        public string SyncSite { get; set; }

        public string SyncUrl { get; set; }

        public DateTime? SyncTime { get; set; }

        public string SyncErrMsg { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }
    }
}
