using BadSecApp.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BadSecApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PersonnesController : Controller
    {
        [HttpPost]
        public IActionResult CreationPersonne([FromBody] Personne personne)
        {
            try
            {
                using (var conn = new SqliteConnection("Data Source=test.db"))
                {
                    conn.Open();
                    var commande = conn.CreateCommand();
                    commande.CommandText = "INSERT INTO PERSONNES (nom, prenom, age) VALUES ('" + personne.Nom + "', '" + personne.Prenom + "', " + personne.Age.ToString() + ")";

                    // De la même manière, une sécurisation simple ici serait la suivante
                    //commande.CommandText = "INSERT INTO PERSONNES (nom, prenom, age) VALUES (@pNom, @pPrenom, @pAge)";
                    //commande.Parameters.Add(new SqliteParameter("pNom", textBox1.Text));
                    //commande.Parameters.Add(new SqliteParameter("pPrenom", textBox2.Text));
                    //commande.Parameters.Add(new SqliteParameter("pAge", numericUpDown1.Value.ToString()));

                    commande.ExecuteNonQuery();
                }
                return new CreatedResult("#", personne);
            }
            catch (Exception ex)
            {
                return new UnprocessableEntityObjectResult(ex.ToString());
            }
        }

        [HttpGet]
        public Tuple<List<Personne>, string> GetAll([FromQuery] string IndicationNom)
        {
            var donnees = new List<Personne>();
            string erreur = string.Empty;

            try
            {
                using (var conn = new SqliteConnection("Data Source=test.db"))
                {
                    conn.Open();
                    var commande = conn.CreateCommand();
                    commande.CommandText = "SELECT nom, prenom, age FROM PERSONNES WHERE nom LIKE '%" + IndicationNom + "%'";

                    // SECU : Rien de tout ceci ne serait arrivé si le code ci-dessous avait remplacé la ligne précédente
                    //commande.CommandText = "SELECT nom, prenom, age FROM PERSONNES WHERE nom LIKE @chaine";
                    //commande.Parameters.Add(new SqliteParameter("chaine", textBox3.Text + "%"));

                    using (var reader = commande.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            donnees.Add(
                                new Personne()
                                {
                                    Nom = reader.GetString(0),
                                    Prenom = reader.GetString(1),
                                    Age = reader.GetInt32(2)
                                });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                erreur = ex.ToString();
            }
            return new Tuple<List<Personne>, string>(donnees, erreur);
        }
    }
}
