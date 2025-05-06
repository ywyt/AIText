using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIText.Controllers
{
    public class BaseController : Controller
    {
        public const string SessionKey = "admin";

        public UserData UserData;

        public UserData GetSession()
        {
            var json = HttpContext.Session.GetString(SessionKey);
            if (json != null)
            {
                var user = Newtonsoft.Json.JsonConvert.DeserializeObject<UserData>(json);
                ViewData["username"] = user;
                return user;
            }
            else
            {
                return null;
            }
        }


        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var userData = GetSession();
            if (userData == null)
            {
                filterContext.Result = Redirect("~/login");
            }
            UserData = userData;
        }
    }

    public class UserData
    {
        public string AdminId { get; set; }

        public string Name { get; set; }

        public bool IsAdmin { get; set; }

    }
}
