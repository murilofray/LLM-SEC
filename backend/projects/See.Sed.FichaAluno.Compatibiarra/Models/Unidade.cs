using See.Sed.GeoApi.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace See.Sed.FichaAluno.Compatibiarra.Models
{
    public sealed class Unidade : ObjetoGeocodificado
    {
        public sealed class Turma
        {
            public readonly Unidade Unidade;
            public readonly int TipoEnsino;
            public readonly int Serie;
            public readonly int CodigoTurma;
            public readonly int NumeroClasse;
            public readonly int Duracao;
            public readonly int TipoClasse;
            public readonly int Turno;
            public readonly int CapacidadeFisicaMaxima;
            public readonly int AlunosPreviamentreMatriculados; // Quantidade de alunos que já estavam efetivamente matriculados na turma
            public readonly DateTime DataInicio;
            public readonly DateTime DataFim;
            public readonly string IdTurma;
            public readonly string Sala;
            public readonly string Descricao;

            // Não precisa serializar
            public readonly Dictionary<int, Aluno> Alunos = new Dictionary<int, Aluno>();
            public int MaiorNrAluno;

            public Turma(Unidade unidade, int tipoEnsino, int serie, int codigoTurma, int duracao, int tipoClasse, int turno, int capacidadeFisicaMaxima, string idTurma, string sala, string descricao, DateTime dataInicio, DateTime dataFim, int numeroClasse, int alunosPreviamentreMatriculados, int maiorNrAluno)
            {
                Unidade = unidade;
                TipoEnsino = tipoEnsino;
                Serie = serie;
                CodigoTurma = codigoTurma;
                Duracao = duracao;
                TipoClasse = tipoClasse;
                Turno = turno;
                CapacidadeFisicaMaxima = capacidadeFisicaMaxima;
                IdTurma = idTurma.Trim().ToUpper();
                Sala = sala.Trim().ToUpper();
                Descricao = descricao.Trim().ToUpper();
                DataInicio = dataInicio;
                DataFim = dataFim;
                NumeroClasse = numeroClasse;
                AlunosPreviamentreMatriculados = alunosPreviamentreMatriculados;
                MaiorNrAluno = maiorNrAluno;
            }

            public override string ToString() => Descricao;

            public bool CapacidadeCompleta => (CapacidadeFisicaMaxima <= (Alunos.Count + AlunosPreviamentreMatriculados));

            public bool CapacidadeEstourada => (CapacidadeFisicaMaxima < (Alunos.Count + AlunosPreviamentreMatriculados));

            public int CapacidadeDisponivel => (CapacidadeFisicaMaxima - (Alunos.Count + AlunosPreviamentreMatriculados));

            public int CapacidadeUtilizada => (Alunos.Count + AlunosPreviamentreMatriculados);

            public bool Vazia => ((Alunos.Count + AlunosPreviamentreMatriculados) == 0);

            public static string DescricaoTipoEnsinoGeral(int tipoEnsino)
            {
                switch (tipoEnsino)
                {
                    case 1: return "ENSINO FUNDAMENTAL";
                    case 2: return "ENSINO MEDIO";
                    case 3: return "EJA FUNDAMENTAL - ANOS INICIAIS";
                    case 4: return "EJA FUNDAMENTAL - ANOS FINAIS";
                    case 5: return "EJA ENSINO MEDIO";
                    case 6: return "EDUCACAO INFANTIL";
                    case 7: return "EDUCACAO ESPECIAL - DA";
                    case 8: return "EDUCACAO ESPECIAL - DF";
                    case 9: return "EDUCACAO ESPECIAL - DI";
                    case 10: return "EDUCACAO ESPECIAL - DV";
                    case 11: return "LINGUA E CULTURA ETNICA - ENS. MEDIO";
                    case 12: return "EDUCACAO ESPECIAL - DEF. AUDITIVO";
                    case 13: return "CURSO NORMAL";
                    case 14: return "ENSINO FUNDAMENTAL DE 9 ANOS";
                    case 15: return "CEL - CENTRO DE ESTUDO DE LINGUAS";
                    case 16: return "DEFICIENCIA MULTIPLA - DMU";
                    case 17: return "PAI - PROGRAMA DE ALFABETIZACAO E INCLUSAO";
                    case 18: return "PEJ - PROJETO ESCOLA DA JUVENTUDE";
                    case 19: return "SALA DE RECURSOS - DEF. MENTAL";
                    case 20: return "RECUPERACAO NAS FÉRIAS";
                    case 21: return "EDUCAÇÃO PROFISSIONAL - ESPECIALIZAÇÃO";
                    case 22: return "EDUCAÇÃO PROFISSIONAL - QUALIFICAÇÃO BÁSICA";
                    case 23: return "EDUCAÇÃO PROFISSIONAL - TÉCNICO - Concomitante";
                    case 24: return "EDUCAÇÃO PROFISSIONAL - TÉCNICO - Subseqüente";
                    case 25: return "ENSINO MEDIO INTEGRADO A EDUCACAO PROFISSIONAL";
                    case 26: return "COMPLEMENTAÇÃO EDUCACIONAL";
                    case 27: return "RECUPERAÇÃO PARALELA DO ENSINO FUNDAMENTAL";
                    case 28: return "RECUPERAÇÃO PARALELA DO ENSINO FUNDAMENTAL";
                    case 29: return "RECUPERAÇÃO PARALELA DO ENSINO MÉDIO";
                    case 30: return "ENSINO FUNDAMENTAL - N1 PRTE";
                    case 31: return "ATIVIDADES CURRICULARES DESPORTIVAS (ACD)";
                    case 32: return "ATENDIMENTO EDUCACIONAL ESPECIALIZADO";
                    case 33: return "EDUCAÇÃO ESPECIAL EXCLUSIVA - CRPE";
                    case 34: return "ESPANHOL";
                    case 35: return "EDUCACAO PROFISSIONAL";
                    case 36: return "PROEJA - ENSINO FUNDAMENTAL";
                    case 37: return "PROEJA - ENSINO MÉDIO";
                    case 38: return "PROJOVEM URBANO";
                    case 39: return "EDUCAÇÃO ESPECIAL - TEA";
                    case 40: return "ENSINO FUNDAMENTAL - N2 PRTE";
                    case 41: return "REF RECUP EF 5º,6º OU 9ºANO/5ªOU8ª SERIE";
                    case 42: return "FEBEM - ENSINO FUNDAMENTAL CICLO I";
                    case 43: return "FEBEM - ENSINO FUNDAMENTAL CICLO II";
                    case 44: return "FEBEM - ENSINO MEDIO";
                    case 45: return "EDUCAÇÃO ESPECIAL - ALTAS HABILIDADES/SUPERDOTAÇÃO – SALA DE RECURSO";
                    case 46: return "EJA TÉCNICO INTEGRADO DE PROFISSIONAL - EF";
                    case 47: return "EJA TECNICO INTEGRADO A EDUC PROF - EM";
                    case 48: return "PROJETO AVENTURA CURRÍCULO - EF";
                    case 49: return "PROJETO AVENTURA CURRÍCULO - EM";
                    case 50: return "ENSINO MEDIO - N3 PRTE";
                    case 51: return "EJA FUNDAMENTAL - ANOS INICIAIS - CEES";
                    case 52: return "EJA FUNDAMENTAL - ANOS FINAIS - CEES";
                    case 53: return "EJA ENSINO MEDIO - CEES";
                    case 54: return "EJA FUNDAMENTAL - ANOS INICIAIS - EAD";
                    case 55: return "EJA FUNDAMENTAL - ANOS FINAIS - EAD";
                    case 56: return "EJA ENSINO MEDIO - EAD";
                    case 57: return "EDUCACAO PROFISSIONAL - EAD";
                    case 58: return "PROJETO EXPLORANDO CURRÍCULO - PEC";
                    case 59: return "PROJETO ESCOLA DA FAMILIA";
                    case 60: return "EDUCAÇÃO FÍSICA DOS ALUNOS DO NOTURNO";
                    case 61: return "EJA FUNDAMENTAL - ANOS INICIAIS - EFI";
                    case 62: return "EJA FUNDAMENTAL - ANOS FINAIS - EFF";
                    case 63: return "EJA ENSINO MEDIO - EEM";
                    case 64: return "EDUCACAO ESPECIAL - DA - SALA DE RECURSO";
                    case 65: return "EEDUCACAO ESPECIAL - DF - SALA DE RECURSO";
                    case 66: return "EDUCACAO ESPECIAL - DI - SALA DE RECURSO";
                    case 67: return "EDUCACAO ESPECIAL - DV - SALA DE RECURSO";
                    case 68: return "TRANSTORNO DO ESPECTRO DO AUTISMO - SALA DE RECURSO";
                    case 69: return "EDUCACAO ESPECIAL - DA - ITINERANTE";
                    case 70: return "EDUCACAO ESPECIAL - DF - ITINERANTE";
                    case 71: return "EDUCACAO ESPECIAL - DI - ITINERANTE";
                    case 72: return "EDUCACAO ESPECIAL - DV - ITINERANTE";
                    case 73: return "TRANSTORNO DO ESPECTRO DO AUTISMO - ITINERANTE";
                    case 74: return "EJA FUNDAMENTAL - ANOS FINAIS - TELECURSO PRESENCIAL";
                    case 75: return "EJA ENSINO MEDIO - TELECURSO PRESENCIAL";
                    case 76: return "ENSINO MEDIO - VENCE";
                    case 77: return "ENSINO FUNDAMENTAL - RC";
                    case 78: return "ENSINO FUNDAMENTAL 9 ANOS - RC";
                    case 79: return "ENSINO FUNDAMENTAL - RCI";
                    case 80: return "ENSINO FUNDAMENTAL 9 ANOS - RCI";
                    case 81: return "ENSINO FUNDAMENTAL 9 ANOS - RC";
                    case 82: return "ENSINO FUNDAMENTAL 9 ANOS - RC";
                    case 83: return "ENSINO FUNDAMENTAL 9 ANOS - RCI";
                    case 84: return "ENSINO FUNDAMENTAL 9 ANOS - RCI";
                    case 85: return "CEEJA";
                    case 86: return "CLASSE HOSPITALAR";
                    case 87: return "EJA FUNDAMENTAL - ANOS FINAIS - MULTISSERIADA";
                    case 88: return "EJA ENSINO MEDIO - MULTISSERIADA";
                }
                return "";
            }

            public string DescricaoTipoEnsino
            {
                get
                {
                    return DescricaoTipoEnsinoGeral(TipoEnsino);
                }
            }

            public string DescricaoTurno
            {
                get
                {
                    switch (Turno)
                    {
                        case (int)Models.Turno.Manha: return "MANHA";
                        case (int)Models.Turno.Intermediario: return "INTERMEDIARIO";
                        case (int)Models.Turno.Tarde: return "TARDE";
                        case (int)Models.Turno.Vespertino: return "VESPERTINO";
                        case (int)Models.Turno.Noite: return "NOITE";
                        case (int)Models.Turno.Integral: return "INTEGRAL";
                    }
                    return "";
                }
            }

            public void Serializar(System.IO.BinaryWriter writer)
            {
                writer.Write(Unidade.CodigoUnidadeCorrigido);
                writer.Write(TipoEnsino);
                writer.Write(Serie);
                writer.Write(CodigoTurma);
                writer.Write(Duracao);
                writer.Write(TipoClasse);
                writer.Write(Turno);
                //@@@
                //writer.Write(CapacidadeFisicaMaximaInicial);
                writer.Write(CapacidadeFisicaMaxima);
                writer.Write(IdTurma);
                writer.Write(Sala);
                writer.Write(Descricao);
                writer.Write(DataInicio.ToBinary());
                writer.Write(DataFim.ToBinary());
                writer.Write(NumeroClasse);
                writer.Write(AlunosPreviamentreMatriculados);
            }

            public static void Deserializar(System.IO.BinaryReader reader, Dictionary<int, Unidade> unidades)
            {
                // Pode ser que a unidade tenha sumido do banco da execução anterior para essa
                int codigoUnidade = reader.ReadInt32();
                Unidade unidade;
                if (unidades.TryGetValue(codigoUnidade, out unidade))
                {
                    // Existe essa separação por causa do formato binário do arquivo original
                    unidade.AdicionarTurma(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadString(), reader.ReadString(), reader.ReadString(), DateTime.FromBinary(reader.ReadInt64()), DateTime.FromBinary(reader.ReadInt64()), reader.ReadInt32(), reader.ReadInt32(), 0, 2);
                }
                else
                {
                    reader.ReadInt32();
                    reader.ReadInt32();
                    reader.ReadInt32();
                    reader.ReadInt32();
                    reader.ReadInt32();
                    reader.ReadInt32();
                    reader.ReadInt32();
                    reader.ReadString();
                    reader.ReadString();
                    reader.ReadString();
                    reader.ReadInt64();
                    reader.ReadInt64();
                    reader.ReadInt32();
                    reader.ReadInt32();
                }
            }
        }

        public readonly int CodigoEscola;
        public readonly int CodigoUnidadeCorrigido;
        public readonly int CodigoUnidadeNoBanco; // Não serializa! Apenas para gravar no banco um dia!
        public readonly int CodigoDiretoriaEstadual;
        public readonly int CodigoMunicipio;
        public readonly int CodigoRedeEnsino;
        public readonly string NomeEscola;
        public readonly string NomeDiretoria;
        public readonly string NomeMunicipio;
        public readonly bool Acessivel;
        public readonly Dictionary<int, int> VagasTiposEnsinoSeries;
        public readonly Dictionary<int, Turma> Turmas;

        // Não precisa serializar
        private Dictionary<int, int> VagasTiposEnsinoSeriesOriginaisDoMunicipio;
        public bool ConsiderarApenasVagasDaUnidade;
        public Dictionary<int, Dictionary<int, int>> QuantidadeDeAlunosDeContinuidadeQueDeveriamEstarNaUnidadePorTipoEnsinoSerie;
        public List<Aluno> AlunosPossiveisPorDistancia;
        public Dictionary<int, Aluno> AlunosAlocadosApenasPorVaga;

        public static int CorrigirCodigoUnidade(int codigoEscola, int codigoUnidade)
        {
            // Existe um BUG no código das unidades, e a unidade 1 é repetida nas escolas 48 e 60045
            return (codigoUnidade != 1 ? codigoUnidade : (int.MinValue + (codigoEscola == 60045 ? 0 : 1)));
        }

        public Unidade(int codigoEscola, int codigoUnidade, int codigoDiretoriaEstadual, int codigoMunicipio, int codigoRedeEnsino, string nomeEscola, string nomeDiretoria, string nomeMunicipio, bool acessivel, double latitude, double longitude) : base(new Coordenada(latitude, longitude))
        {
            CodigoEscola = codigoEscola;
            CodigoUnidadeCorrigido = CorrigirCodigoUnidade(codigoEscola, codigoUnidade);
            CodigoUnidadeNoBanco = (CodigoUnidadeCorrigido > (int.MinValue + 1) ? CodigoUnidadeCorrigido : 1);
            CodigoDiretoriaEstadual = codigoDiretoriaEstadual;
            CodigoMunicipio = codigoMunicipio;
            CodigoRedeEnsino = codigoRedeEnsino;
            NomeEscola = nomeEscola;
            NomeDiretoria = nomeDiretoria;
            NomeMunicipio = nomeMunicipio;
            Acessivel = acessivel;
            VagasTiposEnsinoSeries = new Dictionary<int, int>();
            Turmas = new Dictionary<int, Turma>();
        }

        public void AdicionarTurma(int tipoEnsino, int serie, int codigoTurma, int duracao, int tipoClasse, int turno, int capacidadeFisicaMaxima, string idTurma, string sala, string descricao, DateTime dataInicio, DateTime dataFim, int numeroClasse, int alunosPreviamentreMatriculados, int maiorNrAluno, int CD_STATUS_FLUXO_APROVACAO_TURMA)
        {
            //Não utiliza turmas não homologadas (CD_STATUS_FLUXO_APROVACAO_TURMA == 3)
            if (tipoEnsino <= 0 || serie <= 0 || CD_STATUS_FLUXO_APROVACAO_TURMA == 3)
                return;

            // Existe essa separação por causa do formato binário do arquivo original
            Turma turma = new Turma(this, tipoEnsino, serie, codigoTurma, duracao, tipoClasse, turno, capacidadeFisicaMaxima, idTurma, sala, descricao, dataInicio, dataFim, numeroClasse, alunosPreviamentreMatriculados, maiorNrAluno);
            Turmas[codigoTurma] = turma;

            int chave = TipoEnsinoSerie.Gerar(tipoEnsino, serie);
            int vagas;
            VagasTiposEnsinoSeries.TryGetValue(chave, out vagas);
            vagas += turma.CapacidadeDisponivel;
            VagasTiposEnsinoSeries[chave] = vagas;
        }

        public bool InicialmenteTemVagasParaTipoEnsinoSerie(int tipoEnsino, int serie, bool ensinoIntegral)
        {
            int vagas;
            VagasTiposEnsinoSeries.TryGetValue(TipoEnsinoSerie.Gerar(tipoEnsino, serie), out vagas);
            if (vagas > 0)
            {
                if (!ensinoIntegral)
                    return true;

                // Varre turma por turma, para descobrir se existe ao menos uma turma
                // integral com vagas para o tipo de ensino/série desejados
                foreach (Turma turma in Turmas.Values)
                {
                    if (turma.TipoEnsino == tipoEnsino &&
                        turma.Serie == serie &&
                        turma.Turno == (int)Turno.Integral &&
                        !turma.CapacidadeCompleta)
                        return true;
                }
            }
            return false;
        }

        public override string ToString()
        {
            return $"E {CodigoEscola} {NomeEscola} - {CodigoUnidadeCorrigido} ({Coordenada.ToString()})";
        }

        public void LimparTiposEnsinoETurmas()
        {
            VagasTiposEnsinoSeries.Clear();
            if (VagasTiposEnsinoSeriesOriginaisDoMunicipio != null)
                VagasTiposEnsinoSeriesOriginaisDoMunicipio.Clear();
            Turmas.Clear();
        }

        public int QuantidadeTurmas => Turmas.Count;

        public int QuantidadeTurmasSemCapacidade
        {
            get
            {
                int total = 0;
                foreach (Turma t in Turmas.Values)
                {
                    if (t.CapacidadeFisicaMaxima <= 0)
                        total++;
                }
                return total;
            }
        }

        public int QuantidadeMaximaDeVagasPorTipoEnsinoSerie(int tipoEnsino, int serie)
        {
            int total = 0;
            foreach (Turma turma in Turmas.Values)
            {
                if (turma.TipoEnsino == tipoEnsino && turma.Serie == serie)
                    total += turma.CapacidadeFisicaMaxima;
            }
            return total;
        }

        public string DescricaoRedeEnsino
        {
            get
            {
                switch (CodigoRedeEnsino)
                {
                    case 1: return "ESTADUAL - SE";
                    case 2: return "MUNICIPAL";
                    case 3: return "PRIVADA";
                    case 4: return "FEDERAL";
                    case 5: return "ESTADUAL - OUTROS";
                }
                return "";
            }
        }

        public void Serializar(System.IO.BinaryWriter writer)
        {
            writer.Write(CodigoEscola);
            writer.Write(CodigoUnidadeCorrigido);
            writer.Write(CodigoDiretoriaEstadual);
            writer.Write(CodigoMunicipio);
            writer.Write(CodigoRedeEnsino);
            writer.Write(NomeEscola);
            writer.Write(NomeDiretoria);
            writer.Write(NomeMunicipio);
            writer.Write(Acessivel);
            writer.Write(Coordenada.Latitude);
            writer.Write(Coordenada.Longitude);
            writer.Write(VagasTiposEnsinoSeries.Count);
            foreach (int tipoEnsinoSerie in VagasTiposEnsinoSeries.Keys)
                writer.Write(tipoEnsinoSerie);
        }

        public static Unidade Deserializar(System.IO.BinaryReader reader)
        {
            Unidade unidade = new Unidade(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadString(), reader.ReadString(), reader.ReadString(), reader.ReadBoolean(), reader.ReadDouble(), reader.ReadDouble());
            int total = reader.ReadInt32();
            for (int i = total - 1; i >= 0; i--)
                unidade.VagasTiposEnsinoSeries[reader.ReadInt32()] = 0;
            return unidade;
        }

        public void SerializarTurmas(System.IO.BinaryWriter writer)
        {
            // Existe essa separação por causa do formato binário do arquivo original
            foreach (Turma turma in Turmas.Values)
                turma.Serializar(writer);
        }

        public void EqualizarTurmas(Dictionary<int, List<Turma>> turmasPorTipoEnsinoSerieTurno, List<Aluno> tempAlunos)
        {
            turmasPorTipoEnsinoSerieTurno.Clear();

            // Separa as turmas por tipo de ensino/série/turno
            foreach (Turma turma in Turmas.Values)
            {
                // Não deve equalizar turmas vazias
                if (turma.Vazia)
                    continue;

                // tipoEnsino e serie cabem em 16 bits cada (turno cabe em 3 bits)!
                int chave = turma.TipoEnsino | (turma.Serie << 16) | (turma.Turno << 29);
                List<Turma> lista;
                if (!turmasPorTipoEnsinoSerieTurno.TryGetValue(chave, out lista))
                {
                    lista = new List<Turma>();
                    turmasPorTipoEnsinoSerieTurno[chave] = lista;
                }
                lista.Add(turma);
            }

            // Agora varre todas as listas de turma e tenta equalizar a distribuição
            // de alunos (mas não toca em irmãos alocados na mesma turma)
            foreach (List<Turma> lista in turmasPorTipoEnsinoSerieTurno.Values)
            {
                if (lista.Count <= 1)
                    continue;

                // Calcula a média de alunos entre as turmas 
                int media = 0;
                foreach (Turma turma in lista)
                    media += turma.CapacidadeUtilizada;
                media /= lista.Count;

                // Agora distribui os alunos
                tempAlunos.Clear();

                // Primeiro remove os alunos em excesso
                foreach (Turma turma in lista)
                {
                    while (turma.CapacidadeUtilizada > media)
                    {
                        bool removido = false;
                        // Primeiro tenta remover um aluno de definição ou inscrição
                        // (Respeitando irmãos na mesma turma)
                        foreach (Aluno aluno in turma.Alunos.Values)
                        {
                            if (aluno.TemIrmaoAlocadoNaMesmaTurma || aluno.Continuidade)
                                continue;
                            aluno.TurmaAlocada = null;
                            tempAlunos.Add(aluno);
                            turma.Alunos.Remove(aluno.Codigo);
                            removido = true;
                            break;
                        }
                        if (removido)
                            continue;
                        // Se não foi possível então tenta remover um aluno de continuidade
                        // (Respeitando irmãos na mesma turma)
                        foreach (Aluno aluno in turma.Alunos.Values)
                        {
                            if (aluno.TemIrmaoAlocadoNaMesmaTurma)
                                continue;
                            aluno.TurmaAlocada = null;
                            tempAlunos.Add(aluno);
                            turma.Alunos.Remove(aluno.Codigo);
                            removido = true;
                            break;
                        }
                        if (removido)
                            continue;
                        // Por fim, remove qualquer aluno
                        foreach (Aluno aluno in turma.Alunos.Values)
                        {
                            aluno.TurmaAlocada = null;
                            tempAlunos.Add(aluno);
                            turma.Alunos.Remove(aluno.Codigo);
                            break;
                        }
                    }
                }

                // Agora adiciona os alunos em excesso às turmas com capacidade abaixo da média
                foreach (Turma turma in lista)
                {
                    while (tempAlunos.Count > 0 && turma.CapacidadeUtilizada < media && !turma.CapacidadeCompleta)
                    {
                        Aluno aluno = tempAlunos[tempAlunos.Count - 1];
                        aluno.TurmaAlocada = turma;
                        tempAlunos.RemoveAt(tempAlunos.Count - 1);
                        turma.Alunos.Add(aluno.Codigo, aluno);
                    }
                }

                // Por fim, distribui os demais alunos em todas as turmas, um de cada vez por turma
                bool respeitarCapacidadeMaxima = true;
                while (tempAlunos.Count > 0)
                {
                    bool adicionado = false;
                    foreach (Turma turma in lista)
                    {
                        if (tempAlunos.Count <= 0)
                            break;
                        if (respeitarCapacidadeMaxima && turma.CapacidadeCompleta)
                            continue;
                        Aluno aluno = tempAlunos[tempAlunos.Count - 1];
                        aluno.TurmaAlocada = turma;
                        tempAlunos.RemoveAt(tempAlunos.Count - 1);
                        turma.Alunos.Add(aluno.Codigo, aluno);
                        adicionado = true;
                    }
                    // Algo deu errado, e precisaremos parar de respeitar a capacidade máxima
                    // (Nunca deveria ocorrer)
                    if (!adicionado)
                        respeitarCapacidadeMaxima = false;
                }
            }
        }

        #region Relatório
        public void RelatorioMestre(StringBuilder builder)
        {
            string dir = N(NomeDiretoria);
            string mun = N(NomeMunicipio);
            string nome = N(NomeEscola);
            string rede = N(DescricaoRedeEnsino);

            foreach (Turma turma in Turmas.Values)
                builder.AppendLine($@"""{dir}"",""{mun}"",""{rede}"",{CodigoEscola},""{nome}"",{turma.NumeroClasse},""{N(turma.Descricao)}"",{turma.Serie},""{N(turma.DescricaoTurno)}"",""{N(turma.DescricaoTipoEnsino)}"",{turma.CapacidadeFisicaMaxima},{turma.CapacidadeUtilizada},{(turma.CapacidadeDisponivel)},{(turma.CapacidadeEstourada ? "SIM" : "NÃO")},{turma.CapacidadeUtilizada},{(turma.CapacidadeDisponivel)},{(turma.CapacidadeEstourada ? "SIM" : "NÃO")},{(turma.CapacidadeDisponivel)}");
        }

        public void RelatorioTurmas(StringBuilder builder)
        {
            string dir = N(NomeDiretoria);
            string mun = N(NomeMunicipio);
            string nome = N(NomeEscola);
            string rede = N(DescricaoRedeEnsino);

            if (ConsiderarApenasVagasDaUnidade)
            {
                foreach (KeyValuePair<int, int> par in VagasTiposEnsinoSeriesOriginaisDoMunicipio)
                {
                    int tipoEnsino = TipoEnsinoSerie.TipoEnsino(par.Key);
                    int serie = TipoEnsinoSerie.Serie(par.Key);
                    int vagasAtuais = VagasTiposEnsinoSeries[par.Key];
                    builder.AppendLine($@"""{dir}"",""{mun}"",""{rede}"",{CodigoEscola},""{nome}"",""{N(Turma.DescricaoTipoEnsinoGeral(tipoEnsino))}"",{serie},{par.Value},{(par.Value - vagasAtuais)},{vagasAtuais},{(vagasAtuais < 0 ? "SIM" : "NÃO")}");
                }
            }
            else
            {
                foreach (Turma turma in Turmas.Values)
                    builder.AppendLine($@"""{dir}"",""{mun}"",""{rede}"",{CodigoEscola},""{nome}"",{turma.NumeroClasse},""{N(turma.Descricao)}"",{turma.Serie},""{N(turma.DescricaoTurno)}"",""{N(turma.DescricaoTipoEnsino)}"",{turma.CapacidadeFisicaMaxima},{turma.CapacidadeUtilizada},{(turma.CapacidadeDisponivel)},{(turma.CapacidadeEstourada ? "SIM" : "NÃO")}");
            }

            //foreach (Turma turma in Turmas.Values) {
            //	builder.Append(NomeDiretoria);
            //	builder.Append('\t');
            //	builder.Append(NomeMunicipio);
            //	builder.Append('\t');
            //	builder.Append(CodigoEscola);
            //	builder.Append('\t');
            //	builder.Append(NomeEscola);
            //	builder.Append('\t');
            //	builder.Append(CodigoUnidadeNoBanco);
            //	builder.Append('\t');
            //	builder.Append(CodigoRedeEnsino);
            //	builder.Append('\t');
            //	builder.Append(turma.CodigoTurma);
            //	builder.Append('\t');
            //	builder.Append(turma.Descricao);
            //	builder.Append('\t');
            //	builder.Append(turma.IdTurma);
            //	builder.Append('\t');
            //	builder.Append(turma.Sala);
            //	builder.Append('\t');
            //	builder.Append(turma.TipoEnsino);
            //	builder.Append('\t');
            //	builder.Append(turma.Serie);
            //	builder.Append('\t');
            //	builder.Append(turma.Duracao);
            //	builder.Append('\t');
            //	builder.Append(turma.TipoClasse);
            //	builder.Append('\t');
            //	builder.Append(turma.Turno);
            //	builder.Append('\t');
            //	builder.Append(turma.CapacidadeFisicaMaxima);
            //	builder.Append('\t');
            //	builder.Append(turma.CapacidadeUtilizada);
            //	builder.AppendLine();
            //}
        }

        public void RelatorioContinuidadeAdicionarAluno(int tipoEnsino, int serie)
        {
            if (QuantidadeDeAlunosDeContinuidadeQueDeveriamEstarNaUnidadePorTipoEnsinoSerie == null)
                QuantidadeDeAlunosDeContinuidadeQueDeveriamEstarNaUnidadePorTipoEnsinoSerie = new Dictionary<int, Dictionary<int, int>>();

            Dictionary<int, int> alunosPorSerie;
            if (!QuantidadeDeAlunosDeContinuidadeQueDeveriamEstarNaUnidadePorTipoEnsinoSerie.TryGetValue(tipoEnsino, out alunosPorSerie))
            {
                alunosPorSerie = new Dictionary<int, int>();
                QuantidadeDeAlunosDeContinuidadeQueDeveriamEstarNaUnidadePorTipoEnsinoSerie[tipoEnsino] = alunosPorSerie;
            }

            int total;
            alunosPorSerie.TryGetValue(serie, out total);
            total++;
            alunosPorSerie[serie] = total;
        }

        public void RelatorioContinuidade(StringBuilder builder)
        {
            if (QuantidadeDeAlunosDeContinuidadeQueDeveriamEstarNaUnidadePorTipoEnsinoSerie == null)
                return;

            foreach (KeyValuePair<int, Dictionary<int, int>> alunosPorSerie in QuantidadeDeAlunosDeContinuidadeQueDeveriamEstarNaUnidadePorTipoEnsinoSerie)
            {
                foreach (KeyValuePair<int, int> totais in alunosPorSerie.Value)
                {
                    builder.Append(NomeDiretoria);
                    builder.Append('\t');
                    builder.Append(NomeMunicipio);
                    builder.Append('\t');
                    builder.Append(CodigoEscola);
                    builder.Append('\t');
                    builder.Append(NomeEscola);
                    builder.Append('\t');
                    builder.Append(CodigoUnidadeNoBanco);
                    builder.Append('\t');
                    builder.Append(CodigoRedeEnsino);
                    builder.Append('\t');
                    builder.Append(alunosPorSerie.Key);
                    builder.Append('\t');
                    builder.Append(totais.Key);
                    builder.Append('\t');
                    builder.Append(totais.Value);
                    builder.Append('\t');
                    int real = QuantidadeMaximaDeVagasPorTipoEnsinoSerie(alunosPorSerie.Key, totais.Key);
                    builder.Append(real);
                    builder.Append('\t');
                    builder.Append(real < totais.Value ? "SIM" : "NÃO");
                    builder.AppendLine();
                }
            }
            QuantidadeDeAlunosDeContinuidadeQueDeveriamEstarNaUnidadePorTipoEnsinoSerie.Clear();
            QuantidadeDeAlunosDeContinuidadeQueDeveriamEstarNaUnidadePorTipoEnsinoSerie = null;
        }
        #endregion

        #region SQL
        private static string N(string s)
        {
            return (s == null ? "" : s.Trim().Replace('\"', ' ').Replace('\'', ' ').Replace('\r', ' ').Replace('\n', ' '));
        }

        public void RelatorioMestreSql(DataTable tbl)
        {
            string dir = N(NomeDiretoria);
            string mun = N(NomeMunicipio);
            string nome = N(NomeEscola);
            string rede = N(DescricaoRedeEnsino);

            DataRow row;

            foreach (Turma turma in Turmas.Values)
            {
                row = tbl.NewRow();
                row["CD_DIRETORIA"] = CodigoDiretoriaEstadual;
                row["NM_DIRETORIA"] = dir;
                row["CD_DNE"] = CodigoMunicipio;
                row["NM_MUNICIPIO"] = mun;
                row["CD_REDE_ENSINO"] = CodigoRedeEnsino;
                row["DS_REDE_ENSINO"] = rede;
                row["CD_ESCOLA"] = CodigoEscola;
                row["NM_COMPLETO_ESCOLA"] = nome;
                row["CD_TURMA"] = turma.CodigoTurma;
                row["NR_CLASSE"] = turma.NumeroClasse;
                row["CD_TIPO_ENSINO"] = turma.TipoEnsino;
                row["NM_TIPO_ENSINO"] = turma.DescricaoTipoEnsino;
                row["NR_SERIE"] = turma.Serie;
                row["DS_PERIODO"] = turma.DescricaoTurno;
                row["DS_TURMA"] = turma.Descricao;
                row["ID_CAPA_FISICA_MAX"] = turma.CapacidadeFisicaMaxima;
                row["QTDE_ALOCADO_REAL"] = turma.AlunosPreviamentreMatriculados;
                row["QTDE_ALOCADO_SUGEST"] = turma.CapacidadeUtilizada;
                tbl.Rows.Add(row);
            }
        }
        #endregion

        #region Município
        public void DefinirVagasMunicipais(int tipoEnsino, int serie, int vagas)
        {
            if (!ConsiderarApenasVagasDaUnidade)
            {
                // Limpa as vagas de alguma possível turma que tenha sido adicionada anteriormente
                ConsiderarApenasVagasDaUnidade = true;
                VagasTiposEnsinoSeries.Clear();
                VagasTiposEnsinoSeriesOriginaisDoMunicipio = new Dictionary<int, int>();
            }
            // tipoEnsino e serie cabem em 16 bits cada!
            VagasTiposEnsinoSeries[TipoEnsinoSerie.Gerar(tipoEnsino, serie)] = vagas;
            VagasTiposEnsinoSeriesOriginaisDoMunicipio[TipoEnsinoSerie.Gerar(tipoEnsino, serie)] = vagas;
        }

        public void AlocarAlunoPadreTicao(Aluno aluno)
        {
            aluno.FinalizarAlocacao(this, null, Motivo.PadreTicao);
        }

        public bool TentarAlocarAlunoCEU(Aluno aluno)
        {
            // Tenta colocar dentro de uma turma, mesmo que não usemos a informação
            // de turma para enviar para o Município
            return TentarAlocarAlunoApenasPorVagas(aluno, false, Motivo.CEU);
        }

        public bool TentarAlocarAlunoApenasPorVagas(Aluno aluno, bool apenasSimulacao, Motivo motivo)
        {
            // tipoEnsino e serie cabem em 16 bits cada!
            int chave = TipoEnsinoSerie.Gerar(aluno.TipoEnsinoDesejado, aluno.SerieDesejada), vagas;
            if (!VagasTiposEnsinoSeries.TryGetValue(chave, out vagas) || (vagas <= 0))
                return false;
            if (!apenasSimulacao)
            {
                if (AlunosAlocadosApenasPorVaga == null)
                    AlunosAlocadosApenasPorVaga = new Dictionary<int, Aluno>();
                AlunosAlocadosApenasPorVaga[aluno.Codigo] = aluno;
                vagas--;
                VagasTiposEnsinoSeries[chave] = vagas;
                aluno.FinalizarAlocacao(this, null, motivo);
            }
            return true;
        }
        #endregion

        //        #region Continuidade
        //        public bool TentarAlocarAlunoContinuidade(Aluno aluno, bool ignorarLotacao)
        //        {

        //            // Escolas da rede municipal de SP não passam as turmas, apenas as vagas por tipo de ensino
        //            if (ConsiderarApenasVagasDaUnidade)
        //            {
        //                if (TentarAlocarAlunoApenasPorVagas(aluno, false, Motivo.Continuidade))
        //                    return true;
        //                aluno.Motivo = Motivo.SemVagasNaUnidadeDeContinuidadeMunicipal;
        //                return false;
        //            }

        //            // Primeiro tenta alocar em uma turma de mesma letra (A, B...) no mesmo turno
        //            foreach (Turma turma in Turmas.Values)
        //            {
        //                if (turma.TipoEnsino == aluno.TipoEnsinoDesejado &&
        //                    turma.Serie == aluno.SerieDesejada &&
        //                    turma.Turno == aluno.TurnoDeOrigem &&
        //                    turma.IdTurma == aluno.IdTurmaDeOrigem)
        //                {
        //                    if (turma.CapacidadeCompleta)
        //                        break; // Nesse caso para por aqui, pois estávamos procurando pela mesma letra
        //                    aluno.FinalizarAlocacao(this, turma, Motivo.Continuidade);
        //                    return true;
        //                }
        //            }

        //            // Agora tenta alocar em uma turma de qualquer letra no mesmo turno
        //            foreach (Turma turma in Turmas.Values)
        //            {
        //                if (turma.TipoEnsino == aluno.TipoEnsinoDesejado &&
        //                    turma.Serie == aluno.SerieDesejada &&
        //                    turma.Turno == aluno.TurnoDeOrigem)
        //                {
        //                    if (turma.CapacidadeCompleta)
        //                        continue; // Tenta outra sala
        //                    aluno.FinalizarAlocacao(this, turma, Motivo.Continuidade);
        //                    return true;
        //                }
        //            }

        //            // Por fim, tenta alocar em uma turma de qualquer letra em qualquer turno (exceto noturno)
        //            foreach (Turma turma in Turmas.Values)
        //            {
        //                if (turma.TipoEnsino == aluno.TipoEnsinoDesejado &&
        //                    turma.Serie == aluno.SerieDesejada &&
        //                    turma.Turno != (int)Turno.Noite)
        //                {
        //                    if (turma.CapacidadeCompleta)
        //                        continue; // Tenta outra sala
        //                    aluno.FinalizarAlocacao(this, turma, Motivo.Continuidade);
        //                    return true;
        //                }
        //            }

        //            // Por fim, tenta alocar em uma turma de qualquer letra do noturno
        //            foreach (Turma turma in Turmas.Values)
        //            {
        //                if (turma.TipoEnsino == aluno.TipoEnsinoDesejado &&
        //                    turma.Serie == aluno.SerieDesejada &&
        //                    turma.Turno == (int)Turno.Noite)
        //                {
        //                    if (turma.CapacidadeCompleta)
        //                        continue; // Tenta outra sala
        //                    aluno.FinalizarAlocacao(this, turma, Motivo.Continuidade);
        //                    return true;
        //                }
        //            }

        //#if SIMULAR_ESTADO_INTEIRO
        //			// Tenta alocar em qualquer turma, ignorando a lotação
        //			return (!ignorarLotacao && TentarAlocarAlunoContinuidade(aluno, true));
        //#else

        //            aluno.Motivo = Motivo.SemVagasNaUnidadeDeContinuidade;
        //            return false;
        //#endif
        //        }
        //        #endregion

        #region Definição/Inscrição
        /*public void IniciarPreAlocacaoDefinicaoInscricao() {
			// Limpa todos os dados e prepara para pré-alocar os alunos
			if (AlunosPossiveisPorDistancia != null)
				AlunosPossiveisPorDistancia.Clear();
			AlunosPossiveisPorDistancia = new List<Aluno>(1000);
		}

		public void TerminarPreAlocacaoDefinicaoInscricao() {
			if (AlunosPossiveisPorDistancia == null)
				return;

			// Ordena todos os possíveis alunos dessa unidade por ordem de distância,
			// dos mais próximos para os mais distantes
			AlunosPossiveisPorDistancia.Sort((a, b) => {
				// Não compensa tratar o caso == (que retorna 0), pois muito
				// dificilmente duas distâncias reais são exatamente iguais
				return (a.DistanciaAteUnidade(this) < b.DistanciaAteUnidade(this) ? -1 : 1);
			});
		}*/

        public bool TentarAlocarAlunoDefinicaoInscricao(Aluno aluno, bool apenasSimulacao, bool rodadas, out int turnoAlocado, Motivo motivo, int turnoForcado = (int)Turno.Todos)
        {
            // Não é permitido deslocar um aluno (fases 0, 8 e 9)
            // para a escola onde ele já estuda!
            if (aluno.InscricaoDeslocamentoComOuSemEndereco &&
                aluno.UnidadeAnteriorCorrigido_089 == CodigoUnidadeCorrigido)
            {
                turnoAlocado = (int)Turno.Todos;
                return false;
            }

            if (!rodadas)
            {
                // ***** Nunca devemos alocar um aluno de 6o em uma escola Municipal de SP
                // (exceto quando estivermos processando as rodadas, pois aí já é válido!)
                if (CodigoRedeEnsino == 2 && CodigoMunicipio == 9668 &&
                    aluno.Definicao && aluno.SerieDesejada == 6)
                {
                    turnoAlocado = (int)Turno.Todos;
                    return false;
                }
            }
            //VSTS: 12179 - Não matricular alunos do noturno por alta demanda e retrabalho
            if (aluno.TurnoDesejado == (int)Turno.Noite || aluno.InteresseNoturno == true)
            {
                turnoAlocado = (int)Turno.Todos;
                return false;
            }


            // Escolas da rede municipal de SP não passam as turmas, apenas as vagas por tipo de ensino
            if (ConsiderarApenasVagasDaUnidade)
            {
                turnoAlocado = (int)Turno.Todos;
                // O interesse por ensino integral não é para a rede Municipal de SP
                //10/10/20203 - A pedido da Fatima e Marcio podemos sim compatibilizar integral no municipio de SP
                //if (aluno.InteresseIntegral)
                //    return false;
                return TentarAlocarAlunoApenasPorVagas(aluno, apenasSimulacao, motivo);
            }
            //VSTS: 24797 - Mesmo que tenha vagas nas turmas se for rede 2 e municipio de SP não podemos alocar sem a posição de vagas enviadas pela prefeitura
            if (!(CodigoRedeEnsino == 2 && CodigoMunicipio == 9668))
            {
                // ***** Integral tem prioridade total (deve tentar respeitar, mesmo que extrapole)
                // Primeiro tenta alocar em uma turma conforme os desejos especiais
                // ja foi tentado alocar anteriormente        

                if (aluno.InteresseIntegral || aluno.InscricaoDeslocamentoSemEndereco)
                {
                    foreach (Turma turma in Turmas.Values)
                    {
                        if (turma.TipoEnsino == aluno.TipoEnsinoDesejado &&
                            turma.Serie == aluno.SerieDesejada &&
                            turma.Turno == (int)Turno.Integral &&
                            !turma.CapacidadeCompleta)
                        {
                            turnoAlocado = turma.Turno;
                            if (!apenasSimulacao)
                                aluno.FinalizarAlocacao(this, turma, motivo);
                            return true;
                        }
                    }
                }

                // O integral foi testado antes do turno forçado, pois ele tem precedência,
                // mesmo
                if (turnoForcado > (int)Turno.Todos)
                {
                    // Novo requisito: Não devemos nunca mais alocar automaticamente alunos
                    // de definição/inscrição/transferência no noturno!!!!
                    if (turnoForcado != (int)Turno.Noite)
                    {
                        foreach (Turma turma in Turmas.Values)
                        {
                            if (turma.TipoEnsino == aluno.TipoEnsinoDesejado &&
                                turma.Serie == aluno.SerieDesejada &&
                                turma.Turno == turnoForcado &&
                                !turma.CapacidadeCompleta)
                            {
                                turnoAlocado = turma.Turno;
                                if (!apenasSimulacao)
                                    aluno.FinalizarAlocacao(this, turma, motivo);
                                return true;
                            }
                        }
                    }
                    turnoAlocado = (int)Turno.Todos;
                    return false;
                }

                // ***** Definição/Inscrição com preferência noturna, ignora!

                // Antes de liberar para qualquer turno, tenta alocar em uma turma de manhã, depois à tarde
                foreach (Turma turma in Turmas.Values)
                {
                    if (turma.TipoEnsino == aluno.TipoEnsinoDesejado &&
                        turma.Serie == aluno.SerieDesejada &&
                        turma.Turno == (int)Turno.Manha &&
                        !turma.CapacidadeCompleta)
                    {
                        turnoAlocado = turma.Turno;
                        if (!apenasSimulacao)
                            aluno.FinalizarAlocacao(this, turma, motivo);
                        return true;
                    }
                }
                foreach (Turma turma in Turmas.Values)
                {
                    if (turma.TipoEnsino == aluno.TipoEnsinoDesejado &&
                        turma.Serie == aluno.SerieDesejada &&
                        turma.Turno == (int)Turno.Tarde &&
                        !turma.CapacidadeCompleta)
                    {
                        turnoAlocado = turma.Turno;
                        if (!apenasSimulacao)
                            aluno.FinalizarAlocacao(this, turma, motivo);
                        return true;
                    }
                }

                // Antes de liberar o notuno, tenta qualquer outro turno (exceto integral e noturno)
                foreach (Turma turma in Turmas.Values)
                {
                    if (turma.TipoEnsino == aluno.TipoEnsinoDesejado &&
                        turma.Serie == aluno.SerieDesejada &&
                        turma.Turno != (int)Turno.Integral &&
                        turma.Turno != (int)Turno.Noite &&
                        !turma.CapacidadeCompleta)
                    {
                        turnoAlocado = turma.Turno;
                        if (!apenasSimulacao)
                            aluno.FinalizarAlocacao(this, turma, motivo);
                        return true;
                    }
                }

                // Novo requisito: Não devemos nunca mais alocar automaticamente alunos
                // de definição/inscrição/transferência no noturno!!!!
                //
                // Ignora o turno só como último caso
                //foreach (Turma turma in Turmas.Values) {
                //	if (turma.TipoEnsino == aluno.TipoEnsinoDesejado &&
                //		turma.Serie == aluno.SerieDesejada &&
                //		!turma.CapacidadeCompleta) {
                //		turnoAlocado = turma.Turno;
                //		if (!apenasSimulacao)
                //			aluno.FinalizarAlocacao(this, turma, motivo);
                //		return true;
                //	}
                //}
            }
            turnoAlocado = (int)Turno.Todos;
            return false;
        }

        public bool TentarAlocarAlunoDefinicaoInscricaoIntegral(Aluno aluno, bool apenasSimulacao, bool rodadas, out int turnoAlocado, Motivo motivo, int turnoForcado = (int)Turno.Todos)
        {
            // não devemos alocar aluno com interesse no integral na rede munincipal
            //10/10/20203 - A pedido da Fatima e Marcio podemos sim compatibilizar integral no municipio de SP
            if (ConsiderarApenasVagasDaUnidade && CodigoRedeEnsino == 2 && CodigoMunicipio == 9668 && aluno.InteresseIntegral)
            {
                turnoAlocado = (int)Turno.Todos;
                //10/10/20203 - A pedido da Fatima e Marcio podemos sim compatibilizar integral no municipio de SP
                return TentarAlocarAlunoApenasPorVagas(aluno, apenasSimulacao, motivo);
            }

            if (CodigoRedeEnsino == 2 /*&& CodigoMunicipio != 9668*/)
            {
                turnoAlocado = (int)Turno.Todos;
                return false;
            }

            // Não é permitido deslocar um aluno (fases 0, 8 e 9)
            // para a escola onde ele já estuda!
            if (aluno.InscricaoDeslocamentoComOuSemEndereco &&
            aluno.UnidadeAnteriorCorrigido_089 == CodigoUnidadeCorrigido)
            {
                turnoAlocado = (int)Turno.Todos;
                return false;
            }

            if (!rodadas)
            {
                // ***** Nunca devemos alocar um aluno de 6o em uma escola Municipal de SP
                // (exceto quando estivermos processando as rodadas, pois aí já é válido!)
                if (CodigoRedeEnsino == 2 && CodigoMunicipio == 9668 &&
                    aluno.Definicao && aluno.SerieDesejada == 6)
                {
                    turnoAlocado = (int)Turno.Todos;
                    return false;
                }
            }

            // ***** Integral tem prioridade total (deve tentar respeitar, mesmo que extrapole)
            // Primeiro tenta alocar em uma turma conforme os desejos especiais
            if (aluno.InteresseIntegral)
            {
                foreach (Turma turma in Turmas.Values)
                {
                    if (turma.TipoEnsino == aluno.TipoEnsinoDesejado &&
                        turma.Serie == aluno.SerieDesejada &&
                        turma.Turno == (int)Turno.Integral &&
                        !turma.CapacidadeCompleta)
                    {
                        turnoAlocado = turma.Turno;
                        if (!apenasSimulacao)
                            aluno.FinalizarAlocacao(this, turma, motivo);
                        return true;
                    }
                }
            }

            turnoAlocado = (int)Turno.Todos;
            return false;
        }
        #endregion
    }
}
