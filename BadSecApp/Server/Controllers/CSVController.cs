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
        // A06:2021 – Vulnerable and Outdated Components : Mettre à jour le package CSVHelper
        [HttpGet]
        public int Traiter(string content)
        {
            using (var reader = new StringReader(content))
            using (var csv = new CsvReader(reader))
            using (var csvData = new CsvDataReader(csv))
            {
                return csvData.FieldCount;
            }
        }
    }
}