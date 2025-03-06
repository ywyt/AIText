using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIText
{
    [SugarTable("sys_account")]
    public class SysAccount
    {
        /// <summary>
        /// 主键
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public string AdminId { get; set; }

        public string Name { get; set; }

        public string Pwd { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsOpen { get; set; }

        public bool IsAdmin { get; set; }

    }
}
