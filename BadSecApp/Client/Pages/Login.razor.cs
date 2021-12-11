using BadSecApp.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using System.Net.Http;

namespace BadSecApp.Client.Pages
{
    public class LoginBase : ComponentBase
    {
        protected Credentials ProposedCredentials { get; set; }

        protected string resultat;

        [Inject]
        protected HttpClient http { get; set; }

        [Inject]
        protected IAuthenticationService authenticationService { get; set; }

        [Inject]
        protected NavigationManager Navigation { get; set; }

        protected override void OnInitialized()
        {
            ProposedCredentials = new Credentials();
            base.OnInitialized();
        }

        protected async void Creer()
        {
            bool succes = await authenticationService.Login(ProposedCredentials.login, ProposedCredentials.pwd);
            if (!succes)
                resultat = "Login et/ou mot de passe incorrect";
            else
                Navigation.NavigateTo(ObtenirUrlAccueil());
            this.StateHasChanged();
        }

        private string ObtenirUrlAccueil()
        {
            var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue("returnUrl", out StringValues redirection))
            {
                return redirection;
            }
            return "/Accueil";
        }

    }
}
