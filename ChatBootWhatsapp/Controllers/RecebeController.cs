using ChatBootWhatsapp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.SignalR;
using ChatBootWhatsapp.Hubs;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;

namespace ChatBootWhatsapp.Controllers
{
    public class RecebeController : ControllerBase
    {
        private readonly WhatsappService _whatsappService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<RecebeController> _logger;
        private readonly IConfiguration _config;
        private readonly IMemoryCache _cache;

        public RecebeController(
            WhatsappService whatsappService,
            IHubContext<ChatHub> hubContext,
            ILogger<RecebeController> logger,
            IConfiguration config,
            IMemoryCache cache)
        {
            _whatsappService = whatsappService;
            _hubContext = hubContext;
            _logger = logger;
            _config = config;
            _cache = cache;
        }

        [HttpGet]
        [Route("webhook")]
        public IActionResult Webhook(
            [FromQuery(Name = "hub.mode")] string mode,
            [FromQuery(Name = "hub.challenge")] string challenge,
            [FromQuery(Name = "hub.verify_token")] string verify_token)
        {
            try
            {
                string tokenValido = _config["WhatsappConfig:WebhookToken"] ?? "oi";

                if (verify_token?.Trim().Equals(tokenValido, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return Ok(challenge);
                }

                return Unauthorized();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro na verificação do webhook");
                return StatusCode(500);
            }
        }

        [HttpPost]
        [Route("webhook")]
        public async Task<dynamic> Dados([FromBody] WebHookResponseModel entry)
        {
            try
            {
                var messages = entry?.entry?.FirstOrDefault()?.changes?.FirstOrDefault()?.value?.messages;
                if (messages == null || !messages.Any())
                {
                    _logger.LogInformation("Nenhuma mensagem encontrada no webhook");
                    return new { status = "Sucesso", mensagem = "Evento ignorado." };
                }

                var msg = messages.FirstOrDefault();
                string telefoneWhatsapp = msg?.from;
                string mensagemRecebida = msg?.text?.body;
                string idBotaoClicado = msg?.interactive?.button_reply?.id
                                      ?? msg?.interactive?.list_reply?.id
                                      ?? msg?.button?.text;

                _logger.LogInformation($"Mensagem recebida de {telefoneWhatsapp}: {mensagemRecebida ?? idBotaoClicado}");

                // Armazena a mensagem recebida em cache
                if (!string.IsNullOrEmpty(mensagemRecebida))
                {
                    var cacheKey = $"Mensagens_{telefoneWhatsapp}_{DateTime.Today:yyyyMMdd}";
                    var mensagensDoDia = _cache.Get<List<string>>(cacheKey) ?? new List<string>();
                    mensagensDoDia.Add(mensagemRecebida);
                    _cache.Set(cacheKey, mensagensDoDia, TimeSpan.FromHours(24));
                    _logger.LogInformation($"Mensagem armazenada em cache para {telefoneWhatsapp}");
                }

                // Se for clique em botão
                if (!string.IsNullOrEmpty(idBotaoClicado))
                {
                    string resposta = ObterRespostaPorBotao(idBotaoClicado);
                    bool enviado = await _whatsappService.EnviarMensagemAsync(telefoneWhatsapp, resposta);

                    if (enviado)
                    {
                        await _hubContext.Clients.All.SendAsync("ReceberMensagem", telefoneWhatsapp, resposta);
                        return new { status = "Sucesso", mensagem = "Resposta enviada." };
                    }
                    return new { status = "Erro", mensagem = "Falha ao enviar resposta." };
                }

                // Verificação de pedido confirmado
                bool pedidoConfirmado = await VerificarPedidoConfirmadoCache(telefoneWhatsapp);

                if (pedidoConfirmado)
                {
                    _logger.LogInformation($"Pedido confirmado encontrado para {telefoneWhatsapp}");

                    string mensagemContato = "📞 Seu pedido já está em preparo! Para qualquer urgência ou duvida, entre em contato pelos telefones:\n" +
                                           "(16) 3663-3366 \n(16) 3763-3366 \n(16) 99261-8003";

                    bool enviado = await _whatsappService.EnviarMensagemAsync(telefoneWhatsapp, mensagemContato);

                    if (enviado)
                    {
                        await _hubContext.Clients.All.SendAsync("ReceberMensagem", telefoneWhatsapp, mensagemContato);
                        return new { status = "Sucesso", mensagem = "Mensagem de contato enviada." };
                    }
                    return new { status = "Erro", mensagem = "Falha ao enviar mensagem de contato." };
                }

                // Fluxo normal - Menu de opções
                var botoes = new List<(string id, string titulo)>
                {
                    ("1", "📋 Link Cardápio"),
                    ("2", "📞 Telefones Contato"),
                    ("3", "💳 Chave PIX")
                };

                bool menuEnviado = await _whatsappService.EnviarBotoesRespostaRapidaAsync(
                    telefoneWhatsapp,
                    "Olá! Somos o Tim do Lelê Lanches De Jardinópolis 🍔\nComo podemos ajudar? Selecione uma opção:",
                    botoes);

                if (menuEnviado)
                {
                    await _hubContext.Clients.All.SendAsync("ReceberMensagem", telefoneWhatsapp, "Menu enviado");
                    return new { status = "Sucesso", mensagem = "Menu inicial enviado." };
                }

                return new { status = "Erro", mensagem = "Falha ao enviar menu." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar mensagem");
                return new { status = "Erro", mensagem = "Erro interno no servidor." };
            }
        }

        private async Task<bool> VerificarPedidoConfirmadoCache(string telefone)
        {
            try
            {
                var cacheKey = $"Mensagens_{telefone}_{DateTime.Today:yyyyMMdd}";
                if (!_cache.TryGetValue(cacheKey, out List<string> mensagensDoDia) || mensagensDoDia == null)
                {
                    _logger.LogInformation($"Nenhuma mensagem em cache para {telefone}");
                    return false;
                }

                foreach (var mensagem in mensagensDoDia)
                {
                    // Remove emoji inicial se existir
                    var texto = Regex.Replace(mensagem, @"^\p{Cs}*\p{Cs}", "").Trim();

                    // Verifica combinações chave
                    if (Regex.IsMatch(texto, "(pedido|seu).*(confirmado|será preparado)", RegexOptions.IgnoreCase))
                    {
                        _logger.LogInformation($"Pedido confirmado detectado: {mensagem}");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao verificar pedido confirmado");
                return false;
            }
        }

        private string ObterRespostaPorBotao(string idBotao)
        {
            switch (idBotao)
            {
                case "1":
                    return "🔍 Para facilitar ainda mais seu atendimento criamos um link para melhor atendê-lo.\n\nPor favor, clique no link abaixo para ter acesso ao cardápio e também para fazer seu pedido:\n\nhttps://www.pedidosnozapp.com.br/timdolele\n\n*NÃO RECEBEMOS PEDIDOS PELO WHATSAPP, SOMENTE PELA PLATAFORMA DO LINK!*";

                case "2":
                    return "📱 *Nossos telefones para contato são:*\n(16) 3663-3366 \n(16) 3763-3366 \n(16) 99261-8003";

                case "3":
                    return "💸 *Nossa chave PIX é:* 16992085147\nApós a realização do Pix favor nos mandar o comprovante aqui nesta conversa";

                default:
                    return "❌ Opção inválida. Envie 'oi' para ver o menu novamente.";
            }
        }
    }
}