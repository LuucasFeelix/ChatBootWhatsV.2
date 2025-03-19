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
            string token = "EAAPkLZBJSP9kBO8wYDn2oMErPng4bEGeozq3RuKwISg3jOGh8Jl7urucB85IGVS58AymMByayvS53oIIQ3ZCAPP7D0eqmXfwuYSdAqIKFMC3opOVLwwnit27Ajt6K4ZAcSpxF4RnZBzlNZAoGRZC2UZCHMqrd35e1yccZBZBrfCOiNso7AN2r8TOHmTZBJ2VwtgB3TKLPILjh8GRxnMPR50TIGaPBFXZALI";
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