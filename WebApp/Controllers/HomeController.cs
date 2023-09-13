using IdentityModel.Client;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Globalization;
using WebApp.Models;

namespace WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDiscoveryCache _discoveryCache;
        private readonly IConfiguration configuration;

        public HomeController(IHttpClientFactory httpClientFactory, IDiscoveryCache discoveryCache, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _discoveryCache = discoveryCache;
            this.configuration = configuration;

        }

        public async Task<IActionResult> Company()
        {
            var disco = await _discoveryCache.GetAsync();
            if (disco.IsError) throw new Exception(disco.Error);

            var refreshToken = await HttpContext.GetTokenAsync("access_token");
            var tokenClient = _httpClientFactory.CreateClient();
            tokenClient.BaseAddress = new Uri(configuration["BaseUrl"]);
            tokenClient.SetBearerToken(refreshToken);

            var response = await tokenClient.GetAsync("api/init/companies");
            var responseText = await response.Content.ReadAsStringAsync();
            var companies = JsonConvert.DeserializeObject<List<Company>>(responseText);

            return View(companies);
        }

        [Authorize]
        public IActionResult Claims()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [AllowAnonymous]
        public IActionResult Index() => View();

        public IActionResult SignIn() => Challenge();

        public IActionResult Logout() => SignOut("oidc", "Cookies");

        public async Task<IActionResult> RenewTokens()
        {
            var disco = await _discoveryCache.GetAsync();
            if (disco.IsError) throw new Exception(disco.Error);

            var refreshToken = await HttpContext.GetTokenAsync("refresh_token");
            var tokenClient = _httpClientFactory.CreateClient();

            var tokenResult = await tokenClient.RequestRefreshTokenAsync(new RefreshTokenRequest
            {
                Address = disco.TokenEndpoint,
                ClientId = configuration["AuthSettings:ClientId"],
                ClientSecret = configuration["AuthSettings:ClientSecret"],
                RefreshToken = refreshToken
            });

            if (!tokenResult.IsError)
            {
                var oldIdToken = await HttpContext.GetTokenAsync("id_token");
                var newAccessToken = tokenResult.AccessToken;
                var newRefreshToken = tokenResult.RefreshToken;
                var expiresAt = DateTime.UtcNow + TimeSpan.FromSeconds(tokenResult.ExpiresIn);

                var info = await HttpContext.AuthenticateAsync("Cookies");

                info.Properties.UpdateTokenValue("refresh_token", newRefreshToken);
                info.Properties.UpdateTokenValue("access_token", newAccessToken);
                info.Properties.UpdateTokenValue("expires_at", expiresAt.ToString("o", CultureInfo.InvariantCulture));

                await HttpContext.SignInAsync("Cookies", info.Principal, info.Properties);
                return Redirect("~/Home/Claims");
            }

            ViewData["Error"] = tokenResult.Error;
            return View("Error");
        }
    }
}