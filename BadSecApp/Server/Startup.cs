using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;

namespace BadSecApp.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            // SECU (A04:2021-Insecure Design) : voir plus bas
            services.AddSession();

            services.AddControllersWithViews();
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            using (var conn = new SqliteConnection("Data Source=test.db"))
            {
                conn.Open();
                var commande = conn.CreateCommand();
                commande.CommandText = "DROP TABLE IF EXISTS PERSONNES; CREATE TABLE PERSONNES (nom VARCHAR(32), prenom VARCHAR(32), age INT)";
                commande.ExecuteNonQuery();

                conn.Open();
                commande = conn.CreateCommand();
                commande.CommandText = "DROP TABLE IF EXISTS PHOTOS; CREATE TABLE PHOTOS (nom VARCHAR(32), url VARCHAR(32))";
                commande.ExecuteNonQuery();

                commande = conn.CreateCommand();
                commande.CommandText = "DROP TABLE IF EXISTS CONTRATS; CREATE TABLE CONTRATS (entreprise VARCHAR(32), sujet VARCHAR(32), montant INT)";
                commande.ExecuteNonQuery();

                commande = conn.CreateCommand();
                commande.CommandText = "DROP TABLE IF EXISTS USERS; CREATE TABLE USERS (login VARCHAR(32), hash VARCHAR(32))";
                commande.ExecuteNonQuery();

                commande = conn.CreateCommand();
                commande.CommandText = "INSERT INTO PERSONNES (nom, prenom, age) VALUES ('Lagaffe', 'Gaston', 63)";
                commande.ExecuteNonQuery();

                commande = conn.CreateCommand();
                commande.CommandText = "INSERT INTO PHOTOS (nom, url) VALUES ('Lagaffe', 'gaston.png')";
                commande.ExecuteNonQuery();

                commande = conn.CreateCommand();
                commande.CommandText = "INSERT INTO CONTRATS (entreprise, sujet, montant) VALUES ('Minist�re', 'Contrat ultra-secret', 1000000)";
                commande.ExecuteNonQuery();

                commande = conn.CreateCommand();
                commande.CommandText = "INSERT INTO USERS (login, hash) VALUES ('user', 'f71dbe52628a3f83a77ab494817525c6')"; // SECU (A02:2021-Cryptographic Failures) : donn�e sensible pas assez obfusqu�e (MD5 de toto) et pr�sente dans le code, donc dans le GitHub visible de tous
                /*OGZ
                A02:2021-D�faillances cryptographiques 
                MD5 n'est donc plus consid�r� comme s�r au sens cryptographique.
                Utiliser plutot des algorithmes tels que SHA-256
                */
                commande.ExecuteNonQuery();
            }

            string ChaineConnexion = Configuration.GetConnectionString("DefaultConnection"); // SECU (A04:2021-Insecure Design) : la cha�ne de connexion devrait utiliser la s�curit� int�gr�e et pas afficher le mot de passe en clair
            /*OGZ
            A04: 2021 - Conception non s�curis�e
            Evite de voir passer des mots de passe l�gers
            Avec la s�curit� int�gr�e, les mots de passe sont confi�s � l'Administration Syst�me
            */
            if (ChaineConnexion.Contains("password")) throw new ApplicationException(); // SECU (A05:2021-Security Misconfiguration) : cette tentative de s�curit� est mal faite car elle ne prend pas en compte la casse, et est donc inefficace ; de plus, elle agit sur un symptome plut�t que sur la cause, ce qui n'est pas une bonne pratique
            /*OGZ
            A05:2021-Mauvaise configuration de s�curit� 
            La cha�ne de connexion devrait utiliser la s�curit� int�gr�e
            */

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
            }

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            // SECU (A04:2021-Insecure Design) : inactif par d�faut, toute augmentation de la surface d'attaque peut poser probl�me (ainsi que pour la mont�e en charge, dans ce cas pr�cis, � cause des affinit�s de sessions)
            /*OGZ
            A04:2021-Conception non s�curis�e 
            ?????
            */
            app.UseSession();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
