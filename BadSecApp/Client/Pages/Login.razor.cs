using BadSecApp.Shared;
using Microsoft.AspNetCore.Components;
using System.Net.Http;

namespace BadSecApp.Client.Pages
{
    public class LoginBase : ComponentBase
    {
        protected Credentials ProposedCredentials { get; set; }

        protected string resultat;

        [Inject]
        protected HttpClient http { get; set; }

        protected override void OnInitialized()
        {
            ProposedCredentials = new Credentials();
            base.OnInitialized();
        }

        protected async void Creer()
        {
            // [ACE/LNU] A03:2021-Injection:
            // [ACE/LNU] Cross-Site Scripting - Pas de sanitisation en entrée sur le login, injection de script possible      

            HttpResponseMessage retour = await http.GetAsync("api/Authentication?login=" + ProposedCredentials.login + "&pwd=" + ProposedCredentials.pwd);
            resultat = retour.IsSuccessStatusCode ? "Vous êtes connecté en tant que " + ProposedCredentials.login : "Authentification incorrecte";
            this.StateHasChanged();

            Shared.NavMenu.SetMenusVisibility(
                MenuPersonnesVisible: retour.IsSuccessStatusCode, 
                MenuContratsVisible: retour.IsSuccessStatusCode && ProposedCredentials.login == "admin");
        }
    }
}
