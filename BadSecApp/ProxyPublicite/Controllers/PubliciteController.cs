﻿using Microsoft.AspNetCore.Mvc;
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
            HttpClient client = new HttpClient();
            if (page < 0 || page >= Publicites.Count)
                return "<p>Pas de publicité pour cette fois !</p>";

            return client.GetStringAsync(Publicites[page]).Result;
        }

        [HttpPost]
        public int Post()
        {
            // A10:2021 – Server-Side Request Forgery (SSRF)
            // Request.Body peut contenir une adresse malveillante. Whitelist d'url ?
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
                Publicites.Add(reader.ReadToEndAsync().Result);
            return Publicites.Count - 1;
        }
    }
}
