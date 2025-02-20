using Microsoft.AspNetCore.Mvc;

namespace AIText.Controllers
{
    public class AiAccountController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
