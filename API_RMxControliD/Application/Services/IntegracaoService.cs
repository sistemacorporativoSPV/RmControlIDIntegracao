using API_RMxControliD.Application.DTOs;
using API_RMxControliD.Domain.Entities;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace API_RMxControliD.Application.Services
{
    public class IntegracaoService
    {
        readonly ILogger _logger;

        readonly string _apiRM;
        private readonly APIRHiD _autenticacaoRHiD;
        private readonly int _qtdDiasExecucao;

        public IntegracaoService(ILogger logger, IConfiguration config)
        {
            _logger = logger;

            _apiRM = config["APIRM"];
            _qtdDiasExecucao = string.IsNullOrEmpty(config["APIRHiD:QtdDiasExecucao"]) ? 0 : Int32.Parse(config["APIRHiD:QtdDiasExecucao"]);
            _autenticacaoRHiD = config.GetSection("APIRHiD").Get<APIRHiD>();


        }
        
        public async Task<bool> RealizaIntegracaoAsync()
        {
            try
            {
                //Pega todos os funcionarios no RM, não temos consulta so para demitidos ou admitidos
                var listaFuncionariosRM = await ListaFuncionariosRMAsync();

                //var listaOrdenada = listaFuncionariosRM.OrderByDescending(f => f.DATAADMISSAO);

                //Processa Admitidos
                var listaAdmitidos = RetornaFuncionariosAdmitidos(listaFuncionariosRM);

                if (listaAdmitidos.Any())
                {
                    //Cadastra funcionario no RHiD
                    await ProcessaAdmissaoAsync(listaAdmitidos);
                }
                else
                {
                    _logger.LogInformation($"API_RMxControliD [RealizaIntegracaoAsync] - Não foi encontrado nenhum funcionário admitido");
                }

                //Processa Demitidos
                var listaDemitidos = RetornaFuncionariosDemitidos(listaFuncionariosRM);

                if (listaDemitidos.Any())
                {
                    //Desativa funcionarios no RHiD
                    await ProcessaDemissaoAsync(listaDemitidos);
                }
                else
                {
                    _logger.LogInformation($"API_RMxControliD [RealizaIntegracaoAsync] - Não foi encontrado nenhum funcionário demitido");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Erro] API_RMxControliD - {DateTime.Now.ToString()} - {ex.Message}\n{ex.StackTrace}");
                throw;
            }

        }
        
        private IEnumerable<FuncionarioRM> RetornaFuncionariosAdmitidos(IEnumerable<FuncionarioRM> funcionarios)
        {
            var dataFiltro = DateTime.Today.AddDays(_qtdDiasExecucao);

            _logger.LogInformation($"API_RMxControliD [RetornaFuncionariosAdmitidos] - Procurando funcionários admitidos na data de {dataFiltro.ToLongDateString()} ...");

            var listaFuncionario = funcionarios.ToList().Where(func =>
                (func.DATA_CRIACAO >= dataFiltro && func.DATA_CRIACAO <= dataFiltro.AddDays(1).AddSeconds(-1)) &&
                 CargoBatePonto(func.CARGO));

            //var l = funcionarios.Where(f => f.DATA_CRIACAO != null).OrderByDescending(o => o.DATA_CRIACAO).ToList();

            return listaFuncionario;
        }

        private bool CargoBatePonto(string cargo)
        {
            if (cargo.ToUpper().Contains("DIRETOR") ||
                cargo.ToUpper().Contains("GERENTE") ||
                cargo.ToUpper().Contains("COORDENADOR") ||
                cargo.ToUpper().Contains("ESPECIALISTA") ||
                cargo.ToUpper().Contains("ESTAGIÁRIO"))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public async Task<IEnumerable<FuncionarioRM>> ListaFuncionariosRMAsync()
        {
            using var client = new HttpClient();

            var usuario = "ti.servicos";
            var senha = "4002Lir@";

            var credenciais = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{usuario}:{senha}"));

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credenciais);

            var response = await client.GetAsync(_apiRM);

            var json = await response.Content.ReadAsStringAsync();

            var listaFuncionarios = JsonSerializer.Deserialize<IEnumerable<FuncionarioRM>>(json);

            return listaFuncionarios;
        }

        public async Task<string> CadastraRHiDAsync(FuncionarioRM funcionario)
        {
            try
            {
                var token = await RHiDGetTokenAsync();

                if (String.IsNullOrEmpty(token.accessToken))
                {
                    var msgErro = "[Erro] - API_RMxControliD [CadastraRHiD] - Não foi possível recuperar o Token da api do ControliD";
                    _logger.LogError(msgErro);

                    var exception = new Exception(msgErro);

                    throw exception;
                }

                var idDepartamento = !String.IsNullOrEmpty(funcionario.DEPARTAMENTO) ? await RetornaIdDepartamentoRHiDAsync(funcionario.DEPARTAMENTO) : null;
                
                var idCargo = !String.IsNullOrEmpty(funcionario.CARGO) ? await RetornaIdCargoRHiDAsync(funcionario.CARGO) : null;
                
                var idCentroCusto = !String.IsNullOrEmpty(funcionario.NOME_CENTRO_CUSTO) ? await RetornaIdCentroCustosRHiDAsync(funcionario.NOME_CENTRO_CUSTO) : null;
                
                var funcRHiD = new FuncionarioRHiD
                {
                    status = 1,
                    dateShiftsStartStr = DateTime.Now.ToString("yyyyMMdd"),
                    newIdShift = 1,
                    idCompany = 1,
                    name = funcionario.NOME,
                    registration = funcionario.CHAPA,
                    pis = $"0{funcionario.CPF}",
                    cpf = funcionario.CPF,
                    admissionDateStr = funcionario.DATAADMISSAO.ToString("yyyy-MM-dd") + "T00:00:00",
                    idPersonProfile = 1,
                    idDepartment = idDepartamento.HasValue ? idDepartamento.Value : 0,
                    idPersonRole = idCargo.HasValue ? idCargo.Value : 0,
                    idCostCenter = idCentroCusto.HasValue ? idCentroCusto.Value : 0
                };

                HttpClientHandler clientHandler = new HttpClientHandler();

                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

                var client = new HttpClient(clientHandler);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.accessToken);

                var json = JsonSerializer.Serialize(funcRHiD);

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                //content.ReadAsStringAsync()
                var response = await client.PostAsync($"{_autenticacaoRHiD.BaseUrl}/customerdb/person.svc/a", content);

                var result = await response.Content.ReadAsStringAsync();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"[Erro] - API_RMxControliD [CadastraRHiD] - {DateTime.Now.ToString()} - {ex.Message}\n{ex.StackTrace}");

                throw;
            }


        }

        public async Task<ControliDResponseLogin> RHiDGetTokenAsync()
        {

            HttpClientHandler clientHandler = new HttpClientHandler();

            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

            var client = new HttpClient(clientHandler);

            var payload = new { email = _autenticacaoRHiD.Autenticacao.Usuario, password = _autenticacaoRHiD.Autenticacao.Senha, domain = _autenticacaoRHiD.Autenticacao.Dominio };

            var json = JsonSerializer.Serialize(payload);

            var data = JsonSerializer.Serialize(json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{_autenticacaoRHiD.BaseUrl}/login.svc/", content);

            var result = await response.Content.ReadAsStringAsync();

            return JsonSerializer.Deserialize<ControliDResponseLogin>(result);
        }

        private IEnumerable<FuncionarioRM> RetornaFuncionariosDemitidos(IEnumerable<FuncionarioRM> funcionarios)
        {
            var dataFiltro = DateTime.Today.AddDays(_qtdDiasExecucao);

            _logger.LogInformation($"API_RMxControliD [RetornaFuncionariosDemitidos] - Procurando funcionários demitidos na data de {dataFiltro.ToLongDateString()} ...");

            var listaFuncionario = funcionarios.Where(func =>
                (func.DATADEMISSAO >= dataFiltro && func.DATADEMISSAO <= dataFiltro) &&
                 CargoBatePonto(func.CARGO));

            return listaFuncionario;
        }

        private async Task ProcessaAdmissaoAsync(IEnumerable<FuncionarioRM> listaAdmitidos)
        {

            _logger.LogInformation($"API_RMxControliD [ProcessaAdmissaoAsync] - Verificando funcionários admitidos ...");

            using var enumarator = listaAdmitidos.GetEnumerator();

            var totalCadastrados = 0;

            _logger.LogInformation($"API_RMxControliD [ProcessaAdmissaoAsync] - Encontrado(s) {listaAdmitidos.Count()} admitido(s)");

            while (enumarator.MoveNext())
            {
                var retorno = await CadastraRHiDAsync(enumarator.Current);

                _logger.LogInformation($"API_RMxControliD [ProcessaAdmissao] - {DateTime.Now.ToString()} -  {retorno}");

                totalCadastrados++;
            }

            _logger.LogInformation($"API_RMxControliD [ProcessaAdmissao] - Total Cadastrado(s): {totalCadastrados} admitido(s)");
        }

        private async Task ProcessaDemissaoAsync(IEnumerable<FuncionarioRM> listaDemitidos)
        {
            _logger.LogInformation($"API_RMxControliD [ProcessaDemissaoAsync] - Verificando funcionários demitidos ...");

            using var enumarator = listaDemitidos.GetEnumerator();

            var totalAtualizados = 0;

            _logger.LogInformation($"API_RMxControliD [ProcessaDemissaoAsync] - Encontrado(s) {listaDemitidos.Count()} demitidos");

            while (enumarator.MoveNext())
            {
                var retorno = await AtualizaRHiDAsync(enumarator.Current);

                _logger.LogInformation($"API_RMxControliD [ProcessaDemissao] - {DateTime.Now.ToString()} -  {retorno}");

                totalAtualizados++;

            }

            _logger.LogInformation($"API_RMxControliD [ProcessaDemissao] - Total Desbilitado(s): {totalAtualizados}");
        }

        public async Task<string> AtualizaRHiDAsync(FuncionarioRM funcionario, bool statusAtivo = true)
        {
            var token = await RHiDGetTokenAsync();

            if (String.IsNullOrEmpty(token.accessToken))
            {
                var msgErro = "[Erro] - API_RMxControliD [AtualizaRHiDAsync] - Não foi possível recuperar o Token da api do ControliD";

                _logger.LogError(msgErro);

                var exception = new Exception(msgErro);

                throw exception;
            }

            var funcionarioRHiD = await BuscaFuncionarioRHiDAsync(funcionario);

            if (funcionarioRHiD != null)
            {
                //Atualiza status do funcionario no RHiD para desativado                

                var funcRHiD = new FuncionarioRHiDAtualizacao
                {
                    id = funcionarioRHiD.id,
                    status = statusAtivo ? 1 : 0,
                    dateShiftsStartStr = funcionarioRHiD.dateShiftsStartStr,
                    newIdShift = funcionarioRHiD.newIdShift.HasValue ? funcionarioRHiD.newIdShift.Value : 0,
                    idCompany = funcionarioRHiD.idCompany,
                    name = funcionarioRHiD.name,
                    registration = funcionarioRHiD.registration,
                    pis = funcionarioRHiD.pis.ToString(),
                    cpf = funcionarioRHiD.cpf,
                    admissionDateStr = funcionarioRHiD.admissionDateStr,
                    idPersonProfile = funcionarioRHiD.idPersonProfile
                };

                HttpClientHandler clientHandler = new HttpClientHandler();

                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

                var client = new HttpClient(clientHandler);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.accessToken);

                var jsonAtualizacao = JsonSerializer.Serialize(funcRHiD);

                var content = new StringContent(jsonAtualizacao, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"{_autenticacaoRHiD.BaseUrl}/customerdb/person.svc/a", content);

                var result = await response.Content.ReadAsStringAsync();

                return result;
            }
            else
            {
                var msg = $"[Info] - API_RMxControliD [AtualizaRHiDAsync] Funcionario CPF: {funcionario.CPF} não encontrado no RHiD";

                _logger.LogInformation(msg);

                return msg;
            }
        }

        public async Task<FuncionarioRHiDCadastrado> BuscaFuncionarioRHiDAsync(FuncionarioRM funcionario)
        {
            var token = await RHiDGetTokenAsync();

            if (String.IsNullOrEmpty(token.accessToken))
            {
                var msgErro = "[Erro] - API_RMxControliD [BuscaFuncionarioRHiDAsync] - Não foi possível recuperar o Token da api do ControliD";

                _logger.LogError(msgErro);

                var exception = new Exception(msgErro);

                throw exception;
            }

            _logger.LogInformation($"API_RMxControliD [BuscaFuncionarioRHiDAsync] - Buscando cadastro do funcionário no RHiD ...");

            var request = (HttpWebRequest)WebRequest.Create($"{_autenticacaoRHiD.BaseUrl}/customerdb/person.svc/a_status/tudo");
            request.Method = "POST";
            request.ContentType = "application/json";
            request.Accept = "application/json";

            request.Headers.Add(HttpRequestHeader.Authorization, $"Bearer {token.accessToken}");

            //Recuperar todos os funcionarios

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            ServicePointManager.ServerCertificateValidationCallback =
                (sender, cert, chain, errors) => true;

            using var response = (HttpWebResponse)request.GetResponse();
            using var reader = new StreamReader(response.GetResponseStream());

            var result = reader.ReadToEnd();

            var listaFuncionariosResponse = JsonSerializer.Deserialize<FuncionariosRHiDLista>(result);

            //Filtra o funconario a ser atualizado

            var listaFuncionario = listaFuncionariosResponse.data.Where(func => func.cpf == funcionario.CPF);

            if (listaFuncionario.Any())
            {
                return listaFuncionario.First();
            }
            else
            {
                return null;
            }
        }

        public async Task<string> DeletaRHiDAsync(int id)
        {
            try
            {
                var token = await RHiDGetTokenAsync();

                if (String.IsNullOrEmpty(token.accessToken))
                {
                    var msgErro = "[Erro] - API_RMxControliD [DeletaRHiDAsync] - Não foi possível recuperar o Token da api do ControliD";

                    _logger.LogError(msgErro);

                    var exception = new Exception(msgErro);

                    throw exception;
                }

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                };

                using var client = new HttpClient(handler);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.accessToken);

                var responseDelete = await client.DeleteAsync($"{_autenticacaoRHiD.BaseUrl}/customerdb/person.svc/a/{id}");

                return await responseDelete.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                var msg = $"[Erro] - API_RMxControliD [DeletaRHiDAsync] Funcionario Id: {id} - Erro ao deletar funcionário\n{ex.Message}\n{ex.StackTrace}";

                _logger.LogInformation(msg);

                return msg;
            }
        }

        public async Task<IEnumerable<DepartamentoRHiD>> ListaDepartamentosRHiDAsync()
        {
            try
            {
                var token = await RHiDGetTokenAsync();

                if (String.IsNullOrEmpty(token.accessToken))
                {
                    var msgErro = "[Erro] - API_RMxControliD [ListaDepartamentosRHiDAsync] - Não foi possível recuperar o Token da api do ControliD";

                    _logger.LogError(msgErro);

                    var exception = new Exception(msgErro);

                    throw exception;
                }

                HttpClientHandler clientHandler = new HttpClientHandler();

                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

                var client = new HttpClient(clientHandler);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.accessToken);

                var response = await client.GetAsync($"{_autenticacaoRHiD.BaseUrl}/customerdb/department.svc/a");

                var result = await response.Content.ReadAsStringAsync();

                var listaDepartamentosResponse = JsonSerializer.Deserialize<ListaDepartamentoRHiD>(result);

                return listaDepartamentosResponse.data;
            }
            catch (Exception ex)
            {
                var msg = $"[Erro] - API_RMxControliD [ListaDepartamentosRHiDAsync] - Erro ao listar departamentos\n{ex.Message}\n{ex.StackTrace}";

                _logger.LogInformation(msg);

                throw ex;
            }
        }

        public async Task<IEnumerable<CargoRHiD>> ListaCargosRHiDAsync()
        {
            try
            {
                var token = await RHiDGetTokenAsync();

                if (String.IsNullOrEmpty(token.accessToken))
                {
                    var msgErro = "[Erro] - API_RMxControliD [ListaCargosRHiDAsync] - Não foi possível recuperar o Token da api do ControliD";

                    _logger.LogError(msgErro);

                    var exception = new Exception(msgErro);

                    throw exception;
                }

                HttpClientHandler clientHandler = new HttpClientHandler();

                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

                var client = new HttpClient(clientHandler);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.accessToken);

                var response = await client.GetAsync($"{_autenticacaoRHiD.BaseUrl}/customerdb/personrole.svc/a");

                var result = await response.Content.ReadAsStringAsync();

                var listaCargosResponse = JsonSerializer.Deserialize<ListaCargoRHiD>(result);

                return listaCargosResponse.data;
            }
            catch (Exception ex)
            {
                var msg = $"[Erro] - API_RMxControliD [ListaCargosRHiDAsync] - Erro ao listar departamentos\n{ex.Message}\n{ex.StackTrace}";

                _logger.LogInformation(msg);

                throw ex;
            }
        }

        public async Task<IEnumerable<CentroCustoRHiD>> ListaCentroCustosRHiDAsync()
        {
            try
            {
                var token = await RHiDGetTokenAsync();

                if (String.IsNullOrEmpty(token.accessToken))
                {
                    var msgErro = "[Erro] - API_RMxControliD [ListaCentroCUstosRHiDAsync] - Não foi possível recuperar o Token da api do ControliD";

                    _logger.LogError(msgErro);

                    var exception = new Exception(msgErro);

                    throw exception;
                }

                HttpClientHandler clientHandler = new HttpClientHandler();

                clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => { return true; };

                var client = new HttpClient(clientHandler);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.accessToken);

                var response = await client.GetAsync($"{_autenticacaoRHiD.BaseUrl}/customerdb/costcenter.svc/a");

                var result = await response.Content.ReadAsStringAsync();

                var listaCargosResponse = JsonSerializer.Deserialize<ListaCentroCustoRHiD>(result);

                return listaCargosResponse.data;
            }
            catch (Exception ex)
            {
                var msg = $"[Erro] - API_RMxControliD [ListaCentroCUstosRHiDAsync] - Erro ao listar departamentos\n{ex.Message}\n{ex.StackTrace}";

                _logger.LogInformation(msg);

                throw ex;
            }
        }

        public async Task<int?> RetornaIdDepartamentoRHiDAsync(string departamentoRM)
        {
            var listaDepartamentos = await ListaDepartamentosRHiDAsync();

            if (listaDepartamentos.Any())
            {
                var departamento = listaDepartamentos.Where(d => d.name.ToLower() == departamentoRM.ToLower());

                if (departamento.Any())
                {
                    return departamento.First().id;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

        }

        public async Task<int?> RetornaIdCargoRHiDAsync(string cargoRM)
        {
            var listaCargos = await ListaCargosRHiDAsync();

            if (listaCargos.Any())
            {
                var cargo = listaCargos.Where(d => d.name.ToLower() == cargoRM.ToLower());

                if (cargo.Any())
                {
                    return cargo.First().id;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

        }

        public async Task<int?> RetornaIdCentroCustosRHiDAsync(string centroCustoRM)
        {
            var listaCentroCustos = await ListaCentroCustosRHiDAsync();

            if (listaCentroCustos.Any())
            {
                var centroCusto = listaCentroCustos.Where(d => d.name.ToLower() == centroCustoRM.ToLower());

                if (centroCusto.Any())
                {
                    return centroCusto.First().id;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }

        }
    }
}
