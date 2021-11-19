using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ProxyPublicite.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PubliciteController : ControllerBase
    {
        private static List<string> Publicites = null;
        private HttpClient httpClient;

        public PubliciteController(HttpClient _httpClient)
        {
            if (Publicites == null)
            {
                // Ces adresses sont légitimes, mais pas http://gouigoux.com/attack.html !
                Publicites = new List<string>();
                for (int i = 1; i <= 3; i++)
                    Publicites.Add("http://gouigoux.com/pubs/pub" + i + ".html");
            }

            httpClient = _httpClient;
        }

        [HttpGet]
        public string Get([FromQuery] int page = 0)
        {
            // SECU (A04:2021-Insecure Design) : il faut injecter le client plutôt que le recréer à chaque fois, sinon risque de facilitation de DDOS (ressource lourde à créer)
            if (page < 0 || page >= Publicites.Count)
                return "<p>Pas de publicité pour cette fois !</p>";

            return httpClient.GetStringAsync(Publicites[page]).Result;
        }

        [HttpPost]
        public int Post()
        {
            // SECU (A10:2021-Server-Side Request Forgery) : Cette API permet d'injecter des URLs quelconques qui seront lues comme des publicités, et elle est facile à trouver, même sans Swagger

            var pattern = @"^http://gouigoux.com/pubs/pub[1-9][0-9]*\.html$"; 

            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                var pubUrl = reader.ReadToEndAsync().Result;
                if(new Regex(pattern).IsMatch(pubUrl))
                {
                    Publicites.Add(reader.ReadToEndAsync().Result);
                }
            }
            return Publicites.Count - 1;
        }
    }
}
