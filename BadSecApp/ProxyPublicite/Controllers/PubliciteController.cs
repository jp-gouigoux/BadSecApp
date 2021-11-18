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
            // [ACE/LNU] Question pour JP par rapport à ce que j'ai vu dans MAIN.
            // [ACE/LNU] En stateless, la bonne pratique est de recréer un canal de communication à chaque fois à chaque distant vers un service.
            // [ACE/LNU] Mais en utilisant using pour bien garantir la fermeture à chaque fois...
            // [ACE/LNU] Sommes-nous dans ta même vision lorsque tu indiques qu'il faut passer par injection. Mais quid de la durée de vie que tu envisageais ?
            // [ACE/LNU] Pour nous, il semblerait qu'il soit plus intéressant de conserver le pattern stateless et de garantir les attaques type DoS en amont
            // [ACE/LNU] pour éviter des partages de canaux abusifs ou laissés en vrac, sauf si on introduit un pool de connexion...            
            HttpClient client = new HttpClient();
            if (page < 0 || page >= Publicites.Count)
                return "<p>Pas de publicité pour cette fois !</p>";

            return client.GetStringAsync(Publicites[page]).Result;
        }

        [HttpPost]
        public int Post()
        {
            // [ACE/LNU] A03:2021-Injection: 
            // [ACE/LNU] on prend tout le flux tel quel, on peut donc ajouter le type d'url que l'on veut en guise de publicité
            // [ACE/LNU] Body malveillant envoyé avec Postman
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
                Publicites.Add(reader.ReadToEndAsync().Result);
            return Publicites.Count - 1;
        }
    }
}
