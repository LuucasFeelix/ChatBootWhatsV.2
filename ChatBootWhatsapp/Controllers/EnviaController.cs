using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ChatBootWhatsapp.Controllers
{
    public class EnviaController : ControllerBase
    {
        [HttpGet]
        [Route("envia")]
        public async Task EnviaAsync()
        {
            string token = "EAAPkLZBJSP9kBO0NliithtyMUA9qZCMkhKNVZAxfUhGwlBPB17Gj1Kx7B9xgOaxNhdmdJZAMeTT9ZCJwlYnZBm0UrfpGZAJnXCXj1RE9VO0c54ciCljomChrBliKHV0soPpMkL2RcJdEXZCE7pEZBaWPhwbTXLlPaaZA7DF24OT44HukZBYuKMpswBdCVKQvEGuqs1wNrMLFZBNrkaRJKYGkZCRWVgZBXl5TsZD";
            string idTelefone = "502112759653572";
            string telefone = "5516993837839";

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, $"https://graph.facebook.com/v22.0/{idTelefone}/messages");
            request.Headers.Add("Authorization", "Bearer " + token);
            request.Content = new StringContent("{\"messaging_product\": \"whatsapp\",\"recipient_type\": \"individual\",\"to\": \"" + telefone + "\",\"type\": \"text\",\"text\": {\"body\": \"Esta Funcionando\"}}");
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            HttpResponseMessage response = await client.SendAsync(request);
            string responseBody = await response.Content.ReadAsStringAsync();
        }
    }
}