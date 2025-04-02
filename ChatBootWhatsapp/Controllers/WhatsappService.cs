using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;

namespace ChatBootWhatsapp.Controllers
{
    public class WhatsappService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WhatsappService> _logger;
        private readonly string _token;
        private readonly string _idTelefone;
        private readonly IMemoryCache _cache;

        public WhatsappService(HttpClient httpClient, ILogger<WhatsappService> logger, IConfiguration configuration, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _token = configuration["WhatsappConfig:Token"];
            _idTelefone = configuration["WhatsappConfig:IdTelefone"];
            _cache = cache;
            _logger = logger;
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

            var botoesJson = botoes.Select(botao => $"{{\"type\": \"reply\", \"reply\": {{\"id\": \"{botao.id}\", \"title\": \"{botao.titulo}\"}}}}").ToList();
            var botoesJsonString = string.Join(",", botoesJson);

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

            var request = new HttpRequestMessage(HttpMethod.Post, $"https://graph.facebook.com/v22.0/{_idTelefone}/messages")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> VerificarMensagemHojeAsync(string telefone, string mensagemProcurada, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            try
            {
                var cacheKey = $"MsgRecebida_{telefone}_{DateTime.Today:yyyyMMdd}";
                if (_cache.TryGetValue(cacheKey, out List<string> mensagensRecebidas))
                {
                    return mensagensRecebidas.Any(m => m.Contains(mensagemProcurada, comparisonType));
                }

                var today = DateTime.Today;
                var startTime = new DateTimeOffset(today).ToUnixTimeSeconds();
                var endTime = new DateTimeOffset(today.AddDays(1)).ToUnixTimeSeconds();

                telefone = FormatarNumero(telefone);

                var url = $"https://graph.facebook.com/v18.0/{_idTelefone}/messages?fields=from,text&start_time={startTime}&end_time={endTime}&from={telefone}";

                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);

                HttpResponseMessage response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation($"Resposta da API: {content}");

                    var messages = JsonConvert.DeserializeObject<WhatsappMessagesResponse>(content);

                    return messages?.Data?.Any(m =>
                        m.Text != null &&
                        m.Text.Body.Contains(mensagemProcurada, comparisonType)) ?? false;
                }

                _logger.LogError($"Falha na API: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erro ao verificar mensagem: {mensagemProcurada}");
                return false;
            }
        }

        private class WhatsappMessagesResponse { public List<WhatsappMessage> Data { get; set; } }
        private class WhatsappMessage { public string From { get; set; } public Text Text { get; set; } }
        private class Text { public string Body { get; set; } }

        private string FormatarNumero(string telefone)
        {
            telefone = Regex.Replace(telefone, @"\D", "");
            return telefone.StartsWith("55") ? telefone : "55" + telefone;
        }
    }
}