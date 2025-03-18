using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChatBootWhatsapp.Controllers
{
    public class WhatsappService
    {
        private readonly HttpClient _httpClient;
        private readonly string _token;
        private readonly string _idTelefone;

        public WhatsappService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _token = configuration["WhatsappConfig:Token"];
            _idTelefone = configuration["WhatsappConfig:IdTelefone"];
        }

        public async Task<bool> EnviarMensagemAsync(string telefone, string mensagem)
        {
            telefone = FormatarNumero(telefone);
            mensagem = mensagem.Replace("\r\n", "\\n").Replace("\n", "\\n");

            var json = $"{{\"messaging_product\": \"whatsapp\",\"recipient_type\": \"individual\",\"to\": \"{telefone}\",\"type\": \"text\",\"text\": {{\"body\": \"{mensagem}\"}}}}";

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://graph.facebook.com/v22.0/{_idTelefone}/messages")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> EnviarBotoesRespostaRapidaAsync(string telefone, string mensagem, List<(string id, string titulo)> botoes)
        {
            telefone = FormatarNumero(telefone);
            mensagem = mensagem.Replace("\r\n", "\\n").Replace("\n", "\\n");

            // Cria os botões de resposta rápida
            var botoesJson = botoes.Select(botao => $"{{\"type\": \"reply\", \"reply\": {{\"id\": \"{botao.id}\", \"title\": \"{botao.titulo}\"}}}}").ToList();
            var botoesJsonString = string.Join(",", botoesJson);

            // Monta o JSON para os botões de resposta rápida
            var json = $@"{{
        ""messaging_product"": ""whatsapp"",
        ""recipient_type"": ""individual"",
        ""to"": ""{telefone}"",
        ""type"": ""interactive"",
        ""interactive"": {{
            ""type"": ""button"",
            ""body"": {{
                ""text"": ""{mensagem}""
            }},
            ""action"": {{
                ""buttons"": [{botoesJsonString}]
            }}
        }}
    }}";

            Console.WriteLine($"JSON enviado: {json}"); // Log do JSON

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://graph.facebook.com/v22.0/{_idTelefone}/messages")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

            HttpResponseMessage response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Erro ao enviar mensagem: {error}"); // Log do erro
            }

            return response.IsSuccessStatusCode;
        }

        private string FormatarNumero(string telefone)
        {
            telefone = Regex.Replace(telefone, @"\D", "");
            return telefone.StartsWith("55") ? telefone : "55" + telefone;
        }
    }
}