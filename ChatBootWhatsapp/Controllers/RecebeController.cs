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

namespace ChatBootWhatsapp.Controllers
{
    public class RecebeController : ControllerBase
    {
        private readonly WhatsappService _whatsappService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<RecebeController> _logger;
        private readonly IConfiguration _config;

        public RecebeController(
            WhatsappService whatsappService,
            IHubContext<ChatHub> hubContext,
            ILogger<RecebeController> logger,
            IConfiguration config)
        {
            _whatsappService = whatsappService;
            _hubContext = hubContext;
            _logger = logger;
            _config = config;
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
                _logger.LogInformation("Requisição de verificação recebida");
                
                string tokenValido = _config["WhatsappConfig:WebhookToken"] ?? "oi";
                
                if (verify_token?.Trim().Equals(tokenValido, StringComparison.OrdinalIgnoreCase) == true)
                {
                    _logger.LogInformation("Webhook verificado com sucesso");
                    return Ok(challenge);
                }

                _logger.LogWarning($"Token inválido recebido: {verify_token}");
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
                _logger.LogInformation("Nova mensagem recebida");

                var messages = entry?.entry?.FirstOrDefault()?.changes?.FirstOrDefault()?.value?.messages;
                if (messages == null || !messages.Any())
                {
                    _logger.LogWarning("Mensagem sem conteúdo recebida");
                    return new { status = "Sucesso", mensagem = "Evento ignorado." };
                }

                var msg = messages.FirstOrDefault();
                string telefoneWhatsapp = msg?.from;
                string mensagemRecebida = msg?.text?.body;
                string idBotaoClicado = msg?.interactive?.button_reply?.id 
                                      ?? msg?.interactive?.list_reply?.id 
                                      ?? msg?.button?.text;

                // Se for interação com botão, processa a resposta
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

                // Se for QUALQUER mensagem textual (não apenas "oi" ou "ola"), envia o menu
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

        private string ObterRespostaPorBotao(string idBotao)
        {
            switch (idBotao)
            {
                case "1":
                    return "🔍 Para melhor atendê-los, criamos um link que facilita o acesso ao nosso cardápio de lanches. \n\nhttps://www.pedidosnozapp.com.br/timdolele\n\n ** NÃO RECEBEMOS PEDIDOS PELO WHATSAPP, SOMENTE PELA PLATAFORMA DO LINK! ";
                case "2":
                    return "📱 Nossos telefones para contato são:\n(16) 3663-3366 \n(16) 3763-3366 \n(16) 99261-8003";
                case "3":
                    return "💸 Nossa chave PIX é: 16992085147\nApos a realização do Pix favor nos mandar o comprovante aqui nesta conversa!";
                default:
                    return "❌ Opção inválida. Envie 'oi' para ver o menu novamente.";
            }
        }
    }
}