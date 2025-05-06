using Entitys;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AIText.Controllers
{
    public class LoginController : Controller
    {
        private readonly SqlSugarClient Db;
        public LoginController(SqlSugarClient _Db)
        {
            Db = _Db;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult DoLogin(string account, string pwd)
        {
            if (string.IsNullOrEmpty(account) || string.IsNullOrEmpty(pwd))
            {
                return Json(false);
            }

            string vPwd = Commons.ExtToMD5(pwd);
            var rv = Db.Queryable<SysAccount>().First(it => it.Name == account && it.Pwd == vPwd && it.IsOpen);
            if (rv != null)
            {
                HttpContext.Session.SetString(BaseController.SessionKey, Newtonsoft.Json.JsonConvert.SerializeObject(new UserData()
                {
                    AdminId = rv.AdminId,
                    Name = rv.Name,
                    IsAdmin = rv.IsAdmin
                }));

                return Json(true);
            }
            else
            {
                return Json(false);
            }
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Remove(BaseController.SessionKey);
            return this.Redirect("~/Login");
        }
    }
}
