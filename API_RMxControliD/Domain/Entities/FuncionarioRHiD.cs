namespace API_RMxControliD.Domain.Entities
{
    public class FuncionarioRHiD
    {
        public int status { get; set; } //1 Ativo
        public string dateShiftsStartStr { get; set; } /*Data de ínicio com formato AnoMesDia*/
        public int newIdShift { get; set; }
        public int idCompany { get; set; }
        public string name { get; set; }
        public string registration { get; set; } // Matricula
        public string pis { get; set; }
        public string cpf { get; set; }
        public string admissionDateStr { get; set; } /* Ano-Mes-DiaTimezone*/
        //public string foto { get; set; }
        //public string newPassword { get; set; }
        //public string email { get; set; }
        //public string passwordConfirmation { get; set; }
        public int idPersonProfile { get; set; }
        public int idDepartment { get; set; }
        public int? idPersonRole { get; set; }
        public int? idCostCenter { get; set; }
    }
}
