using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace ProxyPublicite.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PubliciteController : ControllerBase
    {
        private static List<string> Publicites = null;
        private readonly HttpClient httpClient;

        public PubliciteController(HttpClient httpClient)
        {
            if (Publicites == null)
            {
                // Ces adresses sont légitimes, mais pas http://gouigoux.com/attack.html !
                Publicites = new List<string>();
                for (int i = 1; i <= 3; i++)
                    Publicites.Add("http://gouigoux.com/pubs/pub" + i + ".html");
            }

            this.httpClient = httpClient;
        }

        [HttpGet]
        public string Get([FromQuery] int page = 0)
        {
            // SECU (A04:2021-Insecure Design) : il faut injecter le client plutôt que le recréer à chaque fois, sinon risque de facilitation de DDOS (ressource lourde à créer)

            // HttpClient doit être instancié une seule fois et réutilisé tout au long de la vie de l'applicaiton,
            // autrement les sockets système saturent.
            // Voir https://docs.microsoft.com/fr-fr/dotnet/api/system.net.http.httpclient?view=net-5.0
            //HttpClient client = new HttpClient();

            if (page < 0 || page >= Publicites.Count)
                return "<p>Pas de publicité pour cette fois !</p>";

            return httpClient.GetStringAsync(Publicites[page]).Result;
        }

        [HttpPost]
        public int Post()
        {
            // SECU (A10:2021-Server-Side Request Forgery) : Cette API permet d'injecter des URLs quelconques qui seront lues comme des publicités, et elle est facile à trouver, même sans Swagger
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
                Publicites.Add(reader.ReadToEndAsync().Result);
            return Publicites.Count - 1;
        }
    }
}
