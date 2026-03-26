using API_RMxControliD.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Text;
using System.Text.Json;

namespace API_RMxControliD.API.Endpoints.Controllers
{
    [Route("api/integracao")]
    [ApiController]
    public class IntegraController : ControllerBase
    {
        private readonly ILogger<IntegraController> _logger;
        private readonly IntegracaoService _integracao;

        public IntegraController(ILogger<IntegraController> logger, IConfiguration config)
        {
            _logger = logger;
            _integracao = new IntegracaoService(logger, config);
        }

        [HttpGet]
        public async Task<ActionResult> Get()
        {
            _logger.LogInformation($"API_RMxControliD - {DateTime.Now.ToString()} - Teste de api realizado com sucesso");

            return Ok("Teste de api realizado com sucesso");
        }

        [HttpPost]
        public async Task<ActionResult> Post()
        {
            try
            {
                _logger.LogInformation($"API_RMxControliD - {DateTime.Now.ToString()} - Operação Iniciada ...");

                var retorno = await _integracao.RealizaIntegracaoAsync();

                if (retorno)
                {
                    _logger.LogInformation($"API_RMxControliD - {DateTime.Now.ToString()} - Operação finalizada com sucesso");
                    return Ok("Operação finalizada com sucesso");
                }
                else
                {
                    _logger.LogError($"API_RMxControliD - {DateTime.Now.ToString()} - Operação não realizada");
                    return StatusCode(500, "Operação não realizada");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"API_RMxControliD - {DateTime.Now.ToString()} - {ex.Message}\n{ex.StackTrace}");
                return StatusCode(500, ex.Message);

            }

        }

        [HttpGet("login")]
        public async Task<ActionResult> TestaLoginRHiD()
        {

            try
            {
                var json = "{\"email\": \"integracao.homologacao@supervia.com.br\", \"password\": \"Id25Trens\",\"domain\": \"supervia_teste\"}";
                var request = (HttpWebRequest)WebRequest.Create("https://www.rhid.com.br/v2/login.svc/");
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Accept = "application/json";

                var data = Encoding.UTF8.GetBytes(json);
                request.ContentLength = data.Length;

                using (var stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }

                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                
                ServicePointManager.ServerCertificateValidationCallback =
                    (sender, cert, chain, errors) => true;

                using var response = (HttpWebResponse)request.GetResponse();
                using var reader = new StreamReader(response.GetResponseStream());

                var result = reader.ReadToEnd();

                return Ok(result);
            }
            catch (Exception ex)
            {
                var saida = $"{ex.Message}\n\n{ex.StackTrace}";

                return StatusCode(500, saida);
            }
            
        }

        [HttpGet("login/novo")]
        public async Task<ActionResult> TestaLoginHttpClientRHiD()
        {

            try
            {

                HttpClientHandler clientHandler = new HttpClientHandler();
                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

                var client = new HttpClient(clientHandler);

                var json = "{\"email\": \"integracao.homologacao@supervia.com.br\", \"password\": \"Id25Trens\",\"domain\": \"supervia_teste\"}";
                
                var data = JsonSerializer.Serialize(json);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("https://www.rhid.com.br/v2/login.svc/", content);                
                
                var result = await response.Content.ReadAsStringAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                var saida = $"{ex.Message}\n\n{ex.StackTrace}";

                return StatusCode(500, saida);
            }

        }
    }
}
