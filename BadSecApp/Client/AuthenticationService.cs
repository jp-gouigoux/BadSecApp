using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BadSecApp.Client
{
    public interface IAuthenticationService
    {
        Task<bool> Login(string nomUtilisateur, string motdePasse);
        Task Logout();
    }

    public class AuthenticationService : IAuthenticationService
    {
        private const string authenticationKey = "authentification";
        private readonly AuthenticationStateProvider _authenticationStateProvider;
        private readonly HttpClient _httpClient;
        private readonly ISessionStorageService _sessionStorage;
        public AuthenticationService(HttpClient httpClient,
                                    AuthenticationStateProvider authenticationStateProvider,
                                    ISessionStorageService sessionStorage)
        {
            this._httpClient = httpClient;
            this._authenticationStateProvider = authenticationStateProvider;
            this._sessionStorage = sessionStorage;
        }
        public async Task<bool> Login(string login, string password)
        {
            //Obtention du tenant de la session storage
            var tenant = await _sessionStorage.GetItemAsStringAsync("tenant");

            //Appel du controleur d'authentification
            var request = new HttpRequestMessage(HttpMethod.Post, "LoginLegacy/login");
            request.Content = new StringContent(
                                JsonConvert.SerializeObject(new { Login = login, Password = password, Tenant = tenant }),
                                Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                //Récupération de JWT token d'authentification à partir de la réponse
                var json = await response.Content.ReadAsStringAsync();
                dynamic auth = JsonConvert.DeserializeObject(json);
                string jwtToken = auth.token;

                //Utilisation  de l'authentication State Provider pour marquer l'utilisateur comme authentifié
                //en lui passant le JWT token
                await ((LocalAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsAuthenticatedAsync(jwtToken);

                return true;
            }

            return false;

        }

        public async Task Logout()
        {
            await ((LocalAuthenticationStateProvider)_authenticationStateProvider).MarkUserAsLoggedOutAsync();
        }
    }
}
