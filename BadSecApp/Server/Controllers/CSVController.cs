using CsvHelper;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.IO;

namespace BadSecApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CSVController : Controller
    {
        [HttpGet]
        public int Traiter(string content)
        {
            using (var reader = new StringReader(content))
            using (var csv = new CsvReader(reader))
            using (var csvData = new CsvDataReader(csv))
            {
                return csvData.FieldCount; // SECU (A06:2021-Vulnerable and Outdated Components) : ce vieux composant renvoie une exception si aucun champ, ce qui rend beaucoup plus simple un DDOS. Problème car corrigé en 15.0.9, mais on utilise ici un vieux package (d'ailleurs, les autres sont plus récents, mais pas les plus récents).
                /*OGZ
                A06:2021-Composants vulnérables et obsolètes
                Mettre à jour les packages Nuget en permanence : les failles de sécurité découvertes y sont normalement corrigées par l'éditeur
                */
            }
        }
    }
}