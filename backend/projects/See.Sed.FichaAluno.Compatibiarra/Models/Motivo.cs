namespace See.Sed.FichaAluno.Compatibiarra.Models
{
    public enum Motivo : int {
		SemGeoOuSemFicha = 0,
		Continuidade = 1,
		Definicao = 2,
		DefinicaoContinuidade = 3,
		Inscricao = 4,
		ManterJuntoComIrmaoDeContinuidade = 5,
		ManterJuntoComIrmaos = 6,
		SemVagasNaUnidadeDeContinuidade = 7,
		SemVagasNaUnidadeDeContinuidadeMunicipal = 8,
		SemUnidadesCompatibilizadas = 9,
		SemUnidadesDentroDoLimiteDeDistancia = 10,
		SemVagasNasUnidadesDentroDoLimiteDeDistancia = 11,
		CEU = 12,
		PadreTicao = 13,
		SemVagasCEU = 14,
		SemUnidadesCompatibilizadasPadreTicao = 15,
		SemUnidadesDentroDoLimiteDeDistanciaPadreTicao = 16,
		SemVagasNasUnidadesDentroDoLimiteDeDistanciaPadreTicao = 17,
		Congelamento = 18,
		DefinicaoIntegral = 19,
		InscricaoIntegral = 20,
		SemGeo = 21,
		InscricaoDeslocamentoSemEndereco = 22,
		InscricaoDeslocamentoComEndereco = 23,
		InscricaoDeslocamentoSemEnderecoIntegral = 24,
		InscricaoDeslocamentoComEnderecoIntegral = 25,
        SemVagaNaUnidadeDeIntencao = 26
    }
}
