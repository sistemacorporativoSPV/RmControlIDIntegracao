using Microsoft.Extensions.Configuration;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;

namespace RoboRMxControliD
{
    internal class ExecutaIntegracao
    {
        private static readonly Logger _logger = LogManager
        .Setup()
        .LoadConfigurationFromFile("nlog.config")
        .GetCurrentClassLogger();

        public async Task<string> Executar()
        {
            try
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false)
                    .AddEnvironmentVariables()
                    .Build();

                // Chave simples
                var urlApi = configuration["UrlApi"];

                _logger.Info("[RoboRMxControliD] - Executar Integração iniciado");

                using var client = new HttpClient();

                var content = new StringContent("", Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{urlApi}", content);

                var resposta = await response.Content.ReadAsStringAsync();

                _logger.Info($"[RoboRMxControliD] - {resposta}" );
                _logger.Info($"[RoboRMxControliD] - Executar Integração finalizado" );

                return resposta;
            }
            catch (Exception ex)
            {
                _logger.Error($"[RoboRMxControliD] - Ocorreu um erro durante a execução - Erro: {ex.Message}\n{ex.InnerException}");

                throw ex;
            }
        }
    }
}
