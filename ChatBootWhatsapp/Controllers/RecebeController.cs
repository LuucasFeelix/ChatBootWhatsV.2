using ChatBootWhatsapp.Models;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Microsoft.AspNetCore.SignalR;
using ChatBootWhatsapp.Hubs;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ChatBootWhatsapp.Controllers
{
    public class RecebeController : ControllerBase
    {
        private readonly WhatsappService _whatsappService;
        private readonly IHubContext<ChatHub> _hubContext;
        private readonly ILogger<RecebeController> _logger;

        public RecebeController(WhatsappService whatsappService, IHubContext<ChatHub> hubContext, ILogger<RecebeController> logger)
        {
            _whatsappService = whatsappService;
            _hubContext = hubContext;
            _logger = logger;
        }

        [HttpGet]
        [Route("webhook")]
        public string Webhook(
            [FromQuery(Name = "hub.mode")] string mode,
            [FromQuery(Name = "hub.challenge")] string challenge,
            [FromQuery(Name = "hub.verify_token")] string verify_token)
        {
            return verify_token.Equals("oi") ? challenge : "";
        }

        [HttpPost]
        [Route("webhook")]
        public async Task<dynamic> Dados([FromBody] WebHookResponseModel entry)
        {
            var messages = entry?.entry?.FirstOrDefault()?.changes?.FirstOrDefault()?.value?.messages;
            if (messages == null || !messages.Any())
            {
                return new { status = "Sucesso", mensagem = "Evento ignorado." };
            }

            var msg = messages.FirstOrDefault();
            string idWhatsapp = msg?.id;
            string telefoneWhatsapp = msg?.from;
            string mensagemRecebida = msg?.text?.body;
            string idBotaoClicado = null;

            if (msg?.interactive?.button_reply != null)
            {
                idBotaoClicado = msg.interactive.button_reply.id;
            }
            else if (msg?.interactive?.list_reply != null)
            {
                idBotaoClicado = msg.interactive.list_reply.id;
            }
            else if (msg?.button != null)
            {
                idBotaoClicado = msg.button.text;
            }

            if (string.IsNullOrEmpty(mensagemRecebida) && string.IsNullOrEmpty(idBotaoClicado))
            {
                return new { status = "Erro", mensagem = "Mensagem inválida." };
            }

            if (mensagemRecebida?.ToLower() == "oi" || mensagemRecebida?.ToLower() == "ola")
            {
                var botoes = new List<(string id, string titulo)>
                {
                    ("1", "Link Cardapio"),
                    ("2", "Telefones de Contato"),
                    ("3", "Chave Pix")
                };

                bool enviadoComSucesso = await _whatsappService.EnviarBotoesRespostaRapidaAsync(
                    telefoneWhatsapp,
                    "Olá, bem vindo(a)! Somos o Tim do Lelê Lanches de Jardinópolis. \n\nSelecione uma opção:",
                    botoes);

                if (enviadoComSucesso)
                {
                    await _hubContext.Clients.All.SendAsync("ReceberMensagem", telefoneWhatsapp, "Botões de resposta rápida enviados.");
                    return new { status = "Sucesso", mensagem = "Botões de resposta rápida enviados." };
                }
                else
                {
                    return new { status = "Erro", mensagem = "Falha ao enviar botões de resposta rápida." };
                }
            }

            string resposta_whatsapp = "Desculpe, não entendi. Por favor, selecione uma opção válida.";
            string idResposta = !string.IsNullOrEmpty(idBotaoClicado) ? idBotaoClicado : mensagemRecebida;

            switch (idResposta)
            {
                case "1":
                    resposta_whatsapp = "Para melhor atendê-los, criamos um link que facilita o acesso ao nosso cardápio de lanches. \n\n https://www.pedidosnozapp.com.br/timdolele \n\n ** NÃO RECEBEMOS PEDIDOS PELO WHATSAPP, SOMENTE PELA PLATAFORMA DO LINK! **";
                    break;
                case "2":
                    resposta_whatsapp = "Nossos telefones para contato são:\n(16) 3663-3366 \n(16) 3763-3366 \n(16) 99261-8003";
                    break;
                case "3":
                    resposta_whatsapp = "Nossa chave PIX é: 16992085147\nApos a realização do Pix favor nos mandar o comprovante aqui nesta conversa.";
                    break;
            }

            bool respostaEnviada = await _whatsappService.EnviarMensagemAsync(telefoneWhatsapp, resposta_whatsapp);

            if (respostaEnviada)
            {
                await _hubContext.Clients.All.SendAsync("ReceberMensagem", telefoneWhatsapp, resposta_whatsapp);
                return new { status = "Sucesso", mensagem = "Resposta enviada." };
            }
            else
            {
                return new { status = "Erro", mensagem = "Falha ao enviar resposta." };
            }
        }
    }
}