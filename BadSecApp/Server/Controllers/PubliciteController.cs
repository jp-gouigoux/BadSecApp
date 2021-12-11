using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace BadSecApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PubliciteController : Controller
    {
        private IConfiguration configuration;

        private PubliciteController(IConfiguration config)
        {
            configuration = config;
        }

        [HttpGet]
        public Task<string> GetPublicite(int numPub)
        {
            using (HttpClient client = new HttpClient() { BaseAddress = new Uri(configuration["ServeurPublicite"]) })
                return client.GetStringAsync("?page=" + numPub);
        }
    }
}