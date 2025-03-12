using SqlSugar;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuartzTask
{
    public class SqlSugarHelper
    {
        public static string ConnectionString = "";
        public static SqlSugarClient InitDB()
        {
            var config = new ConnectionConfig()
            {
                ConnectionString = ConnectionString,
                DbType = SqlSugar.DbType.MySql,
                IsAutoCloseConnection = true,
            };
            var db = new SqlSugarClient(config);
#if DEBUG
            db.Aop.OnLogExecuting = (sql, pars) =>
            {
                // 开发环境下在vs控制台输出sql
                System.Diagnostics.Debug.WriteLine(sql + "\r\n" + System.Text.Json.JsonSerializer.Serialize(pars.ToDictionary(it => it.ParameterName, it => it.Value)));

                //LogHelper.LogWrite(sql + "\r\n" +Db.Utilities.SerializeObject(pars.ToDictionary(it => it.ParameterName, it => it.Value)));
            };
#endif
            return db;
        }
    }
}
