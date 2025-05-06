using SqlSugar;
using System;

namespace Entitys
{
    /// <summary>
    /// 提示词模板
    /// </summary>
    [SugarTable("prompt_template")]
    public class PromptTemplate
    {
        /// <summary>
        /// 主键
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public System.String Id { get; set; }

        public string Name { get; set; }
        public System.String Prompt { get; set; }

        public bool IsEnable {  get; set; }

        public DateTime CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }
    }
}
