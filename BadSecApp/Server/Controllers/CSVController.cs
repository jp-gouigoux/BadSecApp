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
            // [ACE/LNU] A01:2021-Broken Access Control ou A06:2021-Vulnerable and Outdated Components sur le reader...
            using (var reader = new StringReader(content))
            using (var csv = new CsvReader(reader))
            using (var csvData = new CsvDataReader(csv))
            {
                return csvData.FieldCount;
            }
        }
    }
}