using ChatBootWhatsapp.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace ChatBootWhatsapp.Controllers
{
    public class RecebeController : ControllerBase
    {
        private readonly WhatsappService _whatsappService;

        public RecebeController(WhatsappService whatsappService)
        {
            _whatsappService = whatsappService;
        }

        [HttpGet]
        [Route("webhook")]
        public string Webhook(
            [FromQuery(Name = "hub.mode")] string mode,
            [FromQuery(Name = "hub.challenge")] string challenge,
            [FromQuery(Name = "hub.verify_token")] string verify_token)
        {
            Console.WriteLine($"Webhook chamado: mode={mode}, challenge={challenge}, verify_token={verify_token}");
            return verify_token.Equals("oi") ? challenge : "";
        }

        [HttpPost]
        [Route("webhook")]
        public async Task<dynamic> Dados([FromBody] WebHookResponseModel entry)
        {
            Console.WriteLine("Recebendo dados do webhook...");
            Console.WriteLine($"Payload recebido: {JsonConvert.SerializeObject(entry)}");

            var messages = entry?.entry?.FirstOrDefault()?.changes?.FirstOrDefault()?.value?.messages;
            if (messages == null || !messages.Any())
            {
                Console.WriteLine("Evento recebido não contém mensagens. Ignorando...");
                return new { status = "Sucesso", mensagem = "Evento ignorado." };
            }

            var msg = messages.FirstOrDefault();
            string idWhatsapp = msg?.id;
            string telefoneWhatsapp = msg?.from;

            // Extrai mensagem de texto (se disponível)
            string mensagemRecebida = msg?.text?.body;

            // Extrai resposta do botão (se disponível)
            string idBotaoClicado = null;

            // Verifica se esta é uma mensagem interativa com resposta de botão
            if (msg?.interactive?.button_reply != null)
            {
                idBotaoClicado = msg.interactive.button_reply.id;
                Console.WriteLine($"Botão clicado: {idBotaoClicado}");
            }
            // Verifica se esta é uma mensagem interativa com resposta de lista
            else if (msg?.interactive?.list_reply != null)
            {
                idBotaoClicado = msg.interactive.list_reply.id;
                Console.WriteLine($"Item de lista selecionado: {idBotaoClicado}");
            }
            // Verifica se esta é uma resposta de botão simples
            else if (msg?.button != null)
            {
                idBotaoClicado = msg.button.text;
                Console.WriteLine($"Botão simples clicado: {idBotaoClicado}");
            }

            Console.WriteLine($"Mensagem recebida: {mensagemRecebida}");
            Console.WriteLine($"ID do botão clicado: {idBotaoClicado}");
            Console.WriteLine($"ID WhatsApp: {idWhatsapp}");
            Console.WriteLine($"Telefone WhatsApp: {telefoneWhatsapp}");

            // Se não houver mensagem e nem ID do botão, ignore o evento
            if (string.IsNullOrEmpty(mensagemRecebida) && string.IsNullOrEmpty(idBotaoClicado))
            {
                Console.WriteLine("Mensagem recebida está nula ou vazia.");
                return new { status = "Erro", mensagem = "Mensagem inválida." };
            }

            // Trata mensagens de saudação
            if (mensagemRecebida?.ToLower() == "oi" || mensagemRecebida?.ToLower() == "ola")
            {
                var botoes = new List<(string id, string titulo)>
        {
            ("1", "Link Cardapio"),
            ("2", "Telefones de Contato"),
            ("3", "Chave Pix")
        };

                Console.WriteLine("Preparando para enviar botões de resposta rápida...");
                bool enviadoComSucesso = await _whatsappService.EnviarBotoesRespostaRapidaAsync(
                    telefoneWhatsapp,
                    "Olá, bem vindo(a)! Somos o Tim do Lelê Lanches de Jardinópolis. Selecione uma opção:",
                    botoes);

                if (enviadoComSucesso)
                {
                    Console.WriteLine("Botões de resposta rápida enviados com sucesso.");
                    return new { status = "Sucesso", mensagem = "Botões de resposta rápida enviados." };
                }
                else
                {
                    Console.WriteLine("Falha ao enviar botões de resposta rápida.");
                    return new { status = "Erro", mensagem = "Falha ao enviar botões de resposta rápida." };
                }
            }

            // Trata respostas dos botões
            string resposta_whatsapp = "Desculpe, não entendi. Por favor, selecione uma opção válida.";

            // Usa o ID do botão se disponível, caso contrário usa a mensagem recebida
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

            Console.WriteLine($"Resposta a ser enviada: {resposta_whatsapp}");

            // Envia resposta ao usuário
            bool respostaEnviada = await _whatsappService.EnviarMensagemAsync(telefoneWhatsapp, resposta_whatsapp);

            if (respostaEnviada)
            {
                Console.WriteLine("Resposta enviada com sucesso.");
                return new { status = "Sucesso", mensagem = "Resposta enviada." };
            }
            else
            {
                Console.WriteLine("Falha ao enviar resposta.");
                return new { status = "Erro", mensagem = "Falha ao enviar resposta." };
            }
        }

        private static string ObterValorDaMensagem(WebHookResponseModel entry, Func<Messages, string> selector)
        {
            var msg = entry?.entry?.FirstOrDefault()?.changes?.FirstOrDefault()?.value?.messages?.FirstOrDefault();
            if (msg == null)
            {
                Console.WriteLine("Nenhuma mensagem encontrada no payload. Verifique se o payload contém o campo 'messages'.");
                return null;
            }
            return selector(msg);
        }
    }
}