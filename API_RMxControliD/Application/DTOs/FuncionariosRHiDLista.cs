namespace API_RMxControliD.Application.DTOs
{
    public class FuncionariosRHiDLista
    {
        public List<FuncionarioRHiDCadastrado> data { get; set; }
        public int draw { get; set; }
        public string error { get; set; }
        public bool importAndExport { get; set; }
        public bool isConflict { get; set; }
        public string listColumnNames { get; set; }
        public string listColumnSize { get; set; }
        public string listColumns { get; set; }
        public int recordsFiltered { get; set; }
        public int recordsInDB { get; set; }
        public int recordsToAdd { get; set; }
        public int recordsTotal { get; set; }
        public string status { get; set; }
        public int userLimit { get; set; }
    }
}
