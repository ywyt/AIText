using SqlSugar;
using System;
namespace AIText.Models.DB
{
    [SugarTable("wp_account")]
    public class WpAccount
    {
        /// <summary>
        /// 主键
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public System.String Id { get; set; }
        public System.String Site { get; set; }
        public System.String Username { get; set; }
        public System.String Password { get; set; }
        public System.String AccessKey { get; set; }
        public DateTime? CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
    }
}
