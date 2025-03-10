using System;

namespace AIText.Models.SendRecord
{
    public class SendRecordDto
    {
        public string Id { get; set; }
        public string AiSiteId { get; set; }
        public string AiSite { get; set; }
        public string Link { get; set; }
        public string KeywordId { get; set; }
        public string Keyword { get; set; }
        public string TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime? AiTime { get; set; }
        public string ImgUrl { get; set; }
        public string ImgPath { get; set; }
        public DateTime? ImgTime { get; set; }

        public bool IsSync { get; set; }

        public string SyncSiteId { get; set; }

        public string SyncSite { get; set; }
        public string SyncUrl { get; set; }

        public DateTime? SyncTime { get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }
    }
}
