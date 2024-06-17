
namespace See.Sed.FichaAluno.Compatibiarra.Models
{
    public class Email
    {
        public string emailAluno { get; set; }
        public string emailResponsaveis { get; set; }
        public string nomeAluno { get; set; }
        public string nomeEscola { get; set; }
        public string emailEscola { get; set; }
        public string telefoneEscola { get; set; }
        public string ra { get; set; }
        public string digitoRa { get; set; }
        public string ufRa { get; set; }

        public string RA
        {
            get { return string.Concat(ra, !string.IsNullOrEmpty(digitoRa) ? "-" : "", digitoRa, "/", ufRa); }
        }

        public Email(string EmailAluno, string EmailResponsaveis, string NomeAluno, string NomeEscola, string EmailEscola, string TelefoneEscola, string Ra, string DigitoRa, string UfRa)
        {
            emailAluno = EmailAluno;
            emailResponsaveis = EmailResponsaveis;
            nomeAluno = NomeAluno;
            nomeEscola = NomeEscola;
            emailEscola = EmailEscola;
            telefoneEscola = TelefoneEscola;
            ra = Ra;
            digitoRa = DigitoRa;
            ufRa = UfRa;
        }
    }
}