using See.Sed.GeoApi.Models;
using System;
using System.Collections.Generic;

namespace See.Sed.FichaAluno.Compatibiarra.Models
{
    public class GradeDeObjetos<T> where T : ObjetoGeocodificado {
		//**********************************************************
		// GradeDeObjetos só funciona se Coordenada,
		// Coordenada.X e Coordenada.Y forem imutáveis!!!
		//
		// Para ficar mais correto, Adicionar() deveria
		// recalcular o estado interno (menor, maior etc...),
		// o que não ocorre aqui (as céulas são criadas no
		// construtor, e permanecem até o final da mesma forma)
		//**********************************************************
		private readonly List<T>[,] Grade;
		private readonly int QuantidadeLat, QuantidadeLng;
		public readonly int Total;
		private readonly double MenorLat, MaiorLat, MenorLng, MaiorLng, TamanhoLat, TamanhoLng;

		public GradeDeObjetos(IEnumerable<T> objetosIniciais, double comprimentoLatitudeGraus, double comprimentoLongitudeGraus) {
			double menorLat = double.MaxValue;
			double maiorLat = -double.MaxValue;
			double menorLng = double.MaxValue;
			double maiorLng = -double.MaxValue;

			foreach (T t in objetosIniciais) {
				if (!t.CoordenadaValida)
					continue;
				double m = t.Coordenada.Latitude;
				if (menorLat > m) menorLat = m;
				if (maiorLat < m) maiorLat = m;
				m = t.Coordenada.Longitude;
				if (menorLng > m) menorLng = m;
				if (maiorLng < m) maiorLng = m;
			}

			if (menorLat >= maiorLat || menorLng >= maiorLng) {
				// Nada de interessante aqui (cria uma matriz com 1 linha e 1 coluna)
				Grade = new List<T>[,] { { new List<T>() } };
				QuantidadeLat = 1;
				QuantidadeLng = 1;
				if (menorLat > maiorLat) {
					MenorLat = 0.0;
					MaiorLat = 0.0;
				} else {
					MenorLat = menorLat;
					MaiorLat = maiorLat;
				}
				TamanhoLat = comprimentoLatitudeGraus;
				if (menorLng > maiorLng) {
					MenorLng = 0.0;
					MaiorLng = 0.0;
				} else {
					MenorLng = menorLng;
					MaiorLng = maiorLng;
				}
				TamanhoLng = comprimentoLongitudeGraus;
			} else {
				// Temos que "fazer bater" a quantidade de grupos
				// com o tamanho do grupo pedido
				QuantidadeLat = (int)Math.Ceiling((maiorLat - menorLat) / comprimentoLatitudeGraus);
				QuantidadeLng = (int)Math.Ceiling((maiorLng - menorLng) / comprimentoLongitudeGraus);
				Grade = new List<T>[QuantidadeLat, QuantidadeLng];
				MenorLat = menorLat;
				MaiorLat = maiorLat;
				MenorLng = menorLng;
				MaiorLng = maiorLng;
				TamanhoLat = (maiorLat - menorLat) / QuantidadeLat;
				TamanhoLng = (maiorLng - menorLng) / QuantidadeLng;
			}

			int total = 0;
			foreach (T t in objetosIniciais) {
				if (!t.CoordenadaValida)
					continue;
				total++;
				Adicionar(t);
			}
			Total = total;
		}

		private void CelulaDaCoordenada(Coordenada coordenada, out int linha, out int coluna) {
			// Sempre retorna um índice de célula válido
			int i = (int)((coordenada.Latitude - MenorLat) / TamanhoLat);
			linha = ((i <= 0) ? 0 : ((i >= QuantidadeLat) ? QuantidadeLat - 1 : i));

			i = (int)((coordenada.Longitude - MenorLng) / TamanhoLng);
			coluna = ((i <= 0) ? 0 : ((i >= QuantidadeLng) ? QuantidadeLng - 1 : i));
		}

		public void Adicionar(T objeto) {
			int linha, coluna;
			CelulaDaCoordenada(objeto.Coordenada, out linha, out coluna);
			List<T> lista = Grade[linha, coluna];
			if (lista == null) {
				lista = new List<T>();
				Grade[linha, coluna] = lista;
			}
			lista.Add(objeto);
		}

		public void Adicionar(IEnumerable<T> objetos) {
			foreach (T t in objetos)
				Adicionar(t);
		}

		public void Remover(T objeto) {
			int linha, coluna;
			CelulaDaCoordenada(objeto.Coordenada, out linha, out coluna);
			List<T> lista = Grade[linha, coluna];
			if (lista != null)
				lista.Remove(objeto);
		}

		public void Limpar() {
			for (int linha = QuantidadeLat - 1; linha >= 0; linha--) {
				for (int coluna = QuantidadeLng - 1; coluna >= 0; coluna--) {
					List<T> lista = Grade[linha, coluna];
					if (lista != null) {
						lista.Clear();
						Grade[linha, coluna] = null;
					}
				}
			}
		}

		public List<T> Vizinhos(ObjetoGeocodificado objeto, int nivel) => Vizinhos(objeto.Coordenada, nivel);

		public List<T> Vizinhos(Coordenada coordenada, int nivel) {
			List<T> vizinhos = new List<T>();

			int linhaInicial, colunaInicial, linhaFinal, colunaFinal;
			CelulaDaCoordenada(coordenada, out linhaInicial, out colunaInicial);

			// Se nivel for <= 1, retorna todos os objetos daquela célula (X),
			// e das oito células vizinhas (V)
			// V V V
			// V X V
			// V V V
			//
			// Se nivel for > 1, pula (nivel - 1) "anéis" e traz os vizinhos
			// apenas daquele "anel" (por exemplo, nivel = 2)
			// V V V V V
			// V . . . V
			// V . . . V
			// V . . . V
			// V V V V V
			//
			// Ainda se nivel for > 1, mas estivermos próximo das bordas, as
			// células fora dos limites devem ser ignoradas (por exemplo,
			// nivel = 2, linhaInicial = 1, colunaInicial = 1)
			// . . . V
			// . . . V
			// . . . V
			// V V V V

			if (nivel <= 1) {
				linhaFinal = linhaInicial + 1;
				linhaInicial--;
				colunaFinal = colunaInicial + 1;
				colunaInicial--;

				if (linhaInicial < 0) linhaInicial = 0;
				else if (linhaInicial >= QuantidadeLat) linhaInicial = QuantidadeLat - 1;
				if (linhaFinal < 0) linhaFinal = 0;
				else if (linhaFinal >= QuantidadeLat) linhaFinal = QuantidadeLat - 1;

				if (colunaInicial < 0) colunaInicial = 0;
				else if (colunaInicial >= QuantidadeLng) colunaInicial = QuantidadeLng - 1;
				if (colunaFinal < 0) colunaFinal = 0;
				else if (colunaFinal >= QuantidadeLng) colunaFinal = QuantidadeLng - 1;

				for (int linha = linhaInicial; linha <= linhaFinal; linha++) {
					for (int coluna = colunaInicial; coluna <= colunaFinal; coluna++) {
						List<T> lista = Grade[linha, coluna];
						if (lista != null)
							vizinhos.AddRange(lista);
					}
				}
			} else {
				linhaFinal = linhaInicial + nivel;
				linhaInicial -= nivel;
				colunaFinal = colunaInicial + nivel;
				colunaInicial -= nivel;

				int ci = (colunaInicial <= 0 ? 0 : colunaInicial);
				int cf = (colunaFinal >= QuantidadeLng ? QuantidadeLng - 1 : colunaFinal);

				// Etapa 1: traz os vizinhos da linha superior
				if (linhaInicial >= 0 && linhaInicial < QuantidadeLat) {
					for (int coluna = ci; coluna <= cf; coluna++) {
						List<T> lista = Grade[linhaInicial, coluna];
						if (lista != null)
							vizinhos.AddRange(lista);
					}
				}

				// Etapa 2: traz os vizinhos da linha inferior
				if (linhaFinal >= 0 && linhaFinal < QuantidadeLat) {
					for (int coluna = ci; coluna <= cf; coluna++) {
						List<T> lista = Grade[linhaFinal, coluna];
						if (lista != null)
							vizinhos.AddRange(lista);
					}
				}

				// Agora remove linhaInicial e linhaFinal, para não trazer
				// os mesmos objetos das células dos cantos (que fazem
				// intersecção com as colunas)
				linhaInicial++;
				linhaFinal--;

				int li = (linhaInicial <= 0 ? 0 : linhaInicial);
				int lf = (linhaFinal >= QuantidadeLat ? QuantidadeLat - 1 : linhaFinal);

				// Etapa 3: traz os vizinhos da coluna esquerda
				if (colunaInicial >= 0 && colunaInicial < QuantidadeLng) {
					for (int linha = li; linha <= lf; linha++) {
						List<T> lista = Grade[linha, colunaInicial];
						if (lista != null)
							vizinhos.AddRange(lista);
					}
				}

				// Etapa 4: traz os vizinhos da coluna direita
				if (colunaFinal >= 0 && colunaFinal < QuantidadeLng) {
					for (int linha = li; linha <= lf; linha++) {
						List<T> lista = Grade[linha, colunaFinal];
						if (lista != null)
							vizinhos.AddRange(lista);
					}
				}
			}

			return vizinhos;
		}
	}
}
