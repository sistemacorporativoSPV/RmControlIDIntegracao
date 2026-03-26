namespace API_RMxControliD.Application.DTOs
{
    public class ControliDResponseLogin
    {
        public string accessToken { get; set; }
        public bool expiredPassword { get; set; }
        public bool isPerson { get; set; }
        public object listCustomer { get; set; }
        public bool revendaInadimplente { get; set; }
    }
}
