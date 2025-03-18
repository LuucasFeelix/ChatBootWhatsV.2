using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace ChatBootWhatsapp.Controllers
{
    public class EnviaController : ControllerBase
    {
        [HttpGet]

        [Route("envia")]

        public async Task enviaAsync()
        {
            string token = "EAAPkLZBJSP9kBO0hNnZBclNwBe2RmHH4Vd2djukHXV8ZCDEb8lywZAPxefhQ3qCeppGZAz8bfsu6siLXQZCeXlolwvjwYLPojd3f9cM2rOZAOazmHF4z4tzevcVaxb0zO4FNZBbAZBgdAOyvVkLEZCghDZBkYjKFxXZBDQ4QEiWNYPRZAhsAk8ZBeV2FbaKQnloXwBhcTA0PM26VG5ZCEsIYZBUZAq0iO2ShzqzwZD";

            string idTelefone = "502112759653572";

            string telefone = "5516993837839";
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "https://graph.facebook.com/v22.0/" + idTelefone + "/messages");
            request.Headers.Add("Authorization", "Bearer " + token);
            request.Content = new StringContent("{\"messaging_product\": \"whatsapp\",\"recipient_type\": \"individual\",\"to\": \"" + telefone + "\",\"type\": \"text\",\"text\": {\"body\": \"Esta Funcionando\"}}");
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpResponseMessage response = await client.SendAsync(request);

            string responseBody = await response.Content.ReadAsStringAsync();
        }
    }
}
