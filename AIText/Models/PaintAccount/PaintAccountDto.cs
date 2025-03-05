using System;

namespace AIText.Models.PaintAccount
{
    public class PaintAccountDto
    {
        public System.String Id { get; set; }

        public System.String Site { get; set; }

        public System.String ApiKey { get; set; }

        public bool? IsEnable { get; set; }

        public DateTime? CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }
    }
}
