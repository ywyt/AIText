using System;

namespace AIText.Models.SiteAccount
{
    public class SiteAccountDto
    {
        public System.String Id { get; set; }
        public System.String Site { get; set; }
        public System.String Username { get; set; }
        public System.String Password { get; set; }
        public DateTime? StartDate { get; set; }
        public int CountPerDay { get; set; }
        public string Hours { get; set; }
        public string WcKey { get; set; }
        public string WcSecret { get; set; }
        public bool? IsEnable { get; set; }
        public DateTime? CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
    }
}
