using System;

namespace AIText.Models.WpAccount
{
    public class WpAccountDto
    {
        public System.String Id { get; set; }
        public System.String Site { get; set; }
        public System.String Username { get; set; }
        public System.String Password { get; set; }
        public System.String AccessKey { get; set; }
        public DateTime? CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
    }
}
