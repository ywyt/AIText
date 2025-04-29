using SqlSugar;
using System;
namespace Entitys
{
    [SugarTable("site_account")]
    public class SiteAccount
    {
        /// <summary>
        /// 主键
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public System.String Id { get; set; }
        /// <summary>
        /// 站点类型
        /// </summary>
        public SiteType SiteType { get; set; }
        public System.String Site { get; set; }
        public System.String Username { get; set; }
        public System.String Password { get; set; }
        public System.String AccessKey { get; set; }
        public string WcKey { get; set; }
        public string WcSecret { get; set; }
        public bool IsEnable { get; set; }
        public int CountPerDay { get; set; }
        /// <summary>
        /// 执行任务的时间点
        /// </summary>
        public string Hours { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
    }

    public enum SiteType
    {
        WordPress = 0,
        Shopify = 1
    }
}
