using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ProxyPublicite.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PubliciteController : ControllerBase
    {
        private static List<string> Publicites = null;

        private readonly IPubliciteClient _client;

        public PubliciteController(IPubliciteClient client)
        {
            _client = client;
            if (Publicites == null)
            {
                // Ces adresses sont légitimes, mais pas http://gouigoux.com/attack.html !
                Publicites = new List<string>();
                for (int i = 1; i <= 3; i++)
                    Publicites.Add("http://gouigoux.com/pubs/pub" + i + ".html");
            }
        }

        [HttpGet]
        public string Get([FromQuery] int page = 0)
        {
            if (page < 0 || page >= Publicites.Count)
                return "<p>Pas de publicité pour cette fois !</p>";
            return _client.GetPublicite(Publicites[page]);
        }
    }

    public interface IPubliciteClient
    {
        string GetPublicite(string urlPage);
    }

    public class PubliciteClient : IPubliciteClient
    {
        private readonly HttpClient _client;

        public PubliciteClient(HttpClient client)
        {
            _client = client;
        }

        public string GetPublicite(string urlPage)
        {
            return _client.GetStringAsync(urlPage).Result;
        }
    }
}
