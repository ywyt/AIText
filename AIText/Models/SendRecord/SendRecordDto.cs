using System;

namespace AIText.Models.SendRecord
{
    public class SendRecordDto
    {
        public string Id { get; set; }

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
