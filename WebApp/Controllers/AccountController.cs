using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly IConfiguration _config;

        public AccountController(IConfiguration config)
        {
            _config = config;
        }

        public IActionResult Login(string redirectUri)
        {
            return Challenge(new AuthenticationProperties()
            {
                RedirectUri = "Home/Claims"
            },
           "oidc");
        }

        public IActionResult LogOut(string redirectUri)
        {
            return SignOut(new AuthenticationProperties
            {
                RedirectUri = _config["applicationUrl"]
            },
            OpenIdConnectDefaults.AuthenticationScheme,
            CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }
}
