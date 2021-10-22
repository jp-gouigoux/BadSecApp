using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace BadSecApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CSVController : Controller
    {
        /// <summary>
        /// A06:2021-Vulnerable and Outdated Components => Upgrade CsvHelper nuget package (v11 was 3 years old : outdated and unmaintained)
        /// /!\ We could also wrote our own CSV parser, we don't need a full CSV library only to count columns number !
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        [HttpGet]
        public int Traiter(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                return 0;

            using (var reader = new StringReader(content))
            {
                var line = reader.ReadLine();
                
                return line.Split(';').Length +1;
            }
        }
    }
}