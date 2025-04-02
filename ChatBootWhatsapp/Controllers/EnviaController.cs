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
            string token = "EAAPkLZBJSP9kBO4D4d4U9HdDl3GDmnN14oSwSzYmrzDm0O9DLRcAOCZAmqbT8kj18Qrm1LNUSpLQhD8L6S01eZA6sw7ZAdXzMFGZCqcFN6qC0C1w7mxUKDrMN2FpnWTUMuSD6WNYM5TXBJZBRmWUqMKN58ezkiuy7ukhpRlHaZBrmUE1E8D9dqZC3Ryfm75IAluKU2t03Q2HkCkJPkKfBfhSnzp0gswe";
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