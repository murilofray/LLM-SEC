using Prodesp.DataAccess;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace See.Sed.FichaAluno.Compatibiarra.Models
{
    public static class TipoEnsinoSerie {
		private static Dictionary<int, int> TiposEnsinoSeriesEquivalentes = new Dictionary<int, int>(256);

		public static void CarregarEquivalentes() {
			using (IDataBase db = FactoryDataBase.Create(Program.ConnectionStringRead)) {

				using (SedDataReader reader = db.ExecuteSedReaderCommandText("SELECT CD_TIPO_ENSINO, NR_SERIE, CD_TIPO_ENSINO_EQUIVALENTE, NR_SERIE_EQUIVALENTE FROM CADALUNOS..TB_TP_ENSINO_EQUIVALENCIA WITH (NOLOCK) WHERE DT_EXCL IS NULL")) {
					while (reader.Read())
						TiposEnsinoSeriesEquivalentes[Gerar(reader.GetInt32(0), reader.GetInt32(1))] = Gerar(reader.GetInt32(2), reader.GetInt32(3));
				}
			}
		}

		// TipoEnsino e Serie cabem em 16 bits cada!
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Gerar(int tipoEnsino, int serie) => (tipoEnsino | (serie << 16));

		public static int GerarEquivalente(int tipoEnsinoOriginal, int serieOriginal) => (TiposEnsinoSeriesEquivalentes.TryGetValue(tipoEnsinoOriginal = Gerar(tipoEnsinoOriginal, serieOriginal), out serieOriginal) ? serieOriginal : tipoEnsinoOriginal);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int TipoEnsino(int tipoEnsinoSerie) => (tipoEnsinoSerie & 0xFFFF);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Serie(int tipoEnsinoSerie) => (tipoEnsinoSerie >> 16);
	}
}
