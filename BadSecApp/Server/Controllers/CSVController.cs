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
                return csvData.FieldCount;
            }
        }
    }
}