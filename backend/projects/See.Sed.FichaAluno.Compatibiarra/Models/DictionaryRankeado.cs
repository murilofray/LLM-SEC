using System;
using System.Collections.Generic;

namespace See.Sed.FichaAluno.Compatibiarra.Models
{
    public class DictionaryRankeado<K, V> : IEnumerable<V> {
		private class ItemRankeado {
			public int Rank;
			public K Chave;
			public V Valor;

			public override string ToString() {
				return Valor.ToString();
			}
		}

		private class ItemRankeadoEnumerator : IEnumerator<V> {
			private int Index;
			private List<ItemRankeado> List;

			public ItemRankeadoEnumerator(List<ItemRankeado> list) {
				Index = -1;
				List = list;
			}

			public V Current => List[Index].Valor;

			public void Dispose() => List = null;

			public bool MoveNext() => (++Index < List.Count);

			public void Reset() => Index = -1;

			object System.Collections.IEnumerator.Current => List[Index].Valor;
		}

		private readonly List<ItemRankeado> List;
		private readonly Dictionary<K, ItemRankeado> Dictionary;

		public DictionaryRankeado() {
			List = new List<ItemRankeado>(10);
			Dictionary = new Dictionary<K, ItemRankeado>(10);
		}

		public DictionaryRankeado(int capacidade) {
			List = new List<ItemRankeado>(capacidade);
			Dictionary = new Dictionary<K, ItemRankeado>(capacidade);
		}

		public override string ToString() {
			return $"Count: {Dictionary.Count}";
		}

		public void CopiarDe(DictionaryRankeado<K, V> origem) {
			// Clona os elementos da origem
			List.Clear();
			List.Capacity = origem.List.Capacity;
			List.AddRange(origem.List);

			Dictionary.Clear();
			foreach (KeyValuePair<K, ItemRankeado> p in origem.Dictionary) {
				Dictionary[p.Key] = new ItemRankeado() {
					Rank = p.Value.Rank,
					Chave = p.Value.Chave,
					Valor = p.Value.Valor
				};
			}
		}

		public void Adicionar(K chave, V valor) {
			ItemRankeado item;
			if (Dictionary.TryGetValue(chave, out item)) {
				// Atualizando item existente
				item.Valor = valor;
				return;
			}
			// Novo item
			item = new ItemRankeado() {
				Rank = Dictionary.Count,
				Chave = chave,
				Valor = valor
			};
			List.Add(item);
			Dictionary.Add(chave, item);
		}

		public void Remover(K chave) {
			ItemRankeado item;
			if (!Dictionary.TryGetValue(chave, out item))
				return;
			for (int i = List.Count - 1; i > item.Rank; i--)
				List[i].Rank--;
			List.RemoveAt(item.Rank);
			Dictionary.Remove(chave);
		}

		public void Limpar() {
			List.Clear();
			Dictionary.Clear();
		}

		public void Ordenar(Comparison<V> comparison) {
			List.Sort((a, b) => comparison(a.Valor, b.Valor));
			for (int i = List.Count - 1; i >= 0; i--)
				List[i].Rank = i;
		}

		public bool Contem(K chave) => Dictionary.ContainsKey(chave);

		public int RankDaChave(K chave) => Dictionary[chave].Rank;

		public V ValorDaChave(K chave) => Dictionary[chave].Valor;

		public bool ValorDaChave(K chave, out V valor) {
			ItemRankeado item;
			if (!Dictionary.TryGetValue(chave, out item)) {
				valor = default(V);
				return false;
			}
			valor = item.Valor;
			return true;
		}

		public K ChaveDoRank(int rank) => List[rank].Chave;

		public V ValorDoRank(int rank) => List[rank].Valor;

		public int Quantidade => Dictionary.Count;

		public IEnumerator<V> GetEnumerator() => new ItemRankeadoEnumerator(List);

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => new ItemRankeadoEnumerator(List);
	}
}
