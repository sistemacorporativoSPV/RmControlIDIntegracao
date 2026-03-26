namespace API_RMxControliD.Domain.Entities
{
    public class FuncionarioRM
    {
        public string CHAPA { get; set; }
        public string NOME { get; set; }
        public string CARGO { get; set; }
        public string DEPARTAMENTO { get; set; }
        public string NOME_CENTRO_CUSTO { get; set; }
        public string CENTROCUSTO { get; set; }
        public DateTime DTNASCIMENTO { get; set; }
        public string CPF { get; set; }
        public DateTime DATAADMISSAO { get; set; }
        public DateTime? DATADEMISSAO { get; set; }
        public DateTime? DATA_CRIACAO { get; set; }
    }
}
