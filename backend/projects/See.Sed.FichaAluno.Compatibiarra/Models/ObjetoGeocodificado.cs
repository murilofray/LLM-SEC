using See.Sed.GeoApi.Models;

namespace See.Sed.FichaAluno.Compatibiarra.Models {
	public abstract class ObjetoGeocodificado {
		public readonly Coordenada Coordenada;
		public readonly bool CoordenadaValida; // Não precisa serializar!

		public ObjetoGeocodificado(Coordenada coordenada) {
			Coordenada = coordenada;
			CoordenadaValida = (!double.IsInfinity(coordenada.Latitude) &&
				!double.IsNaN(coordenada.Latitude) &&
				!double.IsInfinity(coordenada.Longitude) &&
				!double.IsNaN(coordenada.Longitude));
		}

		public override string ToString() {
			return Coordenada.ToString();
		}

		public double DistanciaGeodesica(ObjetoGeocodificado destino) {
			return Coordenada.DistanciaGeodesica(destino.Coordenada);
		}
	}
}
