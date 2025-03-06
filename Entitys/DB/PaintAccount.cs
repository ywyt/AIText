using SqlSugar;
using System;

namespace AIText
{
    /// <summary>
    /// 绘图账号
    /// </summary>
    [SugarTable("paint_account")]
    public class PaintAccount
    {
        /// <summary>
        /// 主键
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public System.String Id { get; set; }

        public System.String Site { get; set; }

        /// <summary>
        /// 密钥
        /// </summary>
        public System.String AccessKey { get; set; }

        /// <summary>
        /// 密钥
        /// </summary>
        public System.String SecretKey { get; set; }

        public bool IsEnable {  get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }
    }
}
