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

        public PubliciteController()
        {
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
            // SECU (A04:2021-Insecure Design) : il faut injecter le client plutôt que le recréer à chaque fois, sinon risque de facilitation de DDOS (ressource lourde à créer)
            /*OGZ
            EN recréant le HttpClient, cela peut conduire à un épuisement des ressources, des performances amoindries
            En outre, chaque demande avec un nouveau HttpClient nécessite le rétablissement d'une nouvelle connexion TCP/IP
            Privilégier cette injection de dépendances :
            using Microsoft.AspNetCore.Components;
            [Inject]
            public HttpClient client { get; set; }
            */
            HttpClient client = new HttpClient();
            if (page < 0 || page >= Publicites.Count)
                return "<p>Pas de publicité pour cette fois !</p>";

            return client.GetStringAsync(Publicites[page]).Result;
        }

        [HttpPost]
        public int Post()
        {
            // SECU (A10:2021-Server-Side Request Forgery) : Cette API permet d'injecter des URLs quelconques qui seront lues comme des publicités, et elle est facile à trouver, même sans Swagger
            /*OGZ
            A10:2021-Falsification de requête côté serveur
            En postant des requêtes ciblées à cette API, n'importe quoi pourrait être affiché dans la page.
            Peut-être faut-il implémenter CORS pour prévenir l'accès à cette API cross-domain ?
            Comment protéger nos API ? En s'authentifiant : cette API ne doit pas être accessible sans une authentification forte
            */
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
                Publicites.Add(reader.ReadToEndAsync().Result);
            return Publicites.Count - 1;
        }
    }
}
