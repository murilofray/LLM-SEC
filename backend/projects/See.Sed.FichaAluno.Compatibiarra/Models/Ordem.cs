namespace See.Sed.FichaAluno.Compatibiarra.Models
{
    public class Ordem
    {
        public long id { get; set; }
        public double distancia { get; set; }
        public int codigoAluno { get; set; }
        public int codigoEscola { get; set; }
        public int codigoUnidade { get; set; }
        public string MotivoStr { get; set; }
        public bool continuidade { get; set; }
        public long codigoEnderecoAluno { get; set; }
    }
}
