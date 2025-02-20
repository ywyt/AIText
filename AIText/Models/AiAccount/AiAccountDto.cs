using System;

namespace AIText.Models.AiAccount
{
    public class AiAccountDto
    {
        public System.String Id { get; set; }

        public System.String Site { get; set; }

        public System.String ApiKey { get; set; }

        public int IsEnable { get; set; }

        public DateTime? CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }
    }
}
