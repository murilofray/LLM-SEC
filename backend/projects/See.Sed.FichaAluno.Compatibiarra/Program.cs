using Prodesp.DataAccess;
using See.Sed.FichaAluno.Compatibiarra.Models;
using See.Sed.FichaAluno.Compatibiarra.SQL;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace See.Sed.FichaAluno.Compatibiarra
{
    class Program
    {
        public static bool Rodadas;
        private const double DeltaLatLng = 0.001;
        internal const string ConnectionStringRead = "ConnectionStringSqlRead";
        internal const string ConnectionStringWrite = "ConnectionStringSqlWrite";

        public static int AnoLetivo = int.Parse(ConfigurationManager.AppSettings["AnoLetivo"].ToString());
        public static string AnoLetivoStr = ConfigurationManager.AppSettings["AnoLetivo"].ToString();
        public static string FasesCompatibilizacaoCalculo2 = ConfigurationManager.AppSettings["FasesCompatibilizacaoCalculo2"].ToString();
        public static string DNECompatibilizacao = ConfigurationManager.AppSettings["DNECompatibilizacao"].ToString();
        public static string NotDNECompatibilizacao = ConfigurationManager.AppSettings["NotDNECompatibilizacao"].ToString();
        public static bool UtilizarDataLimiteMinimaDasFichasDeInscricaoNasRodadas = bool.Parse(ConfigurationManager.AppSettings["UtilizarDataLimiteMinimaDasFichasDeInscricaoNasRodadas"].ToString());
        public static string DataLimiteMinimaDasFichasDeInscricaoNasRodadas = ConfigurationManager.AppSettings["DataLimiteMinimaDasFichasDeInscricaoNasRodadas"].ToString();
        public static bool UtilizarDataLimiteMaximaDasFichasDeInscricaoNasRodadas = bool.Parse(ConfigurationManager.AppSettings["UtilizarDataLimiteMaximaDasFichasDeInscricaoNasRodadas"].ToString());
        public static string DataLimiteMaximaDasFichasDeInscricaoNasRodadas = ConfigurationManager.AppSettings["DataLimiteMaximaDasFichasDeInscricaoNasRodadas"].ToString();
        public static bool AlocarAlunosNasVagasMunicipaisDeSP = bool.Parse(ConfigurationManager.AppSettings["AlocarAlunosNasVagasMunicipaisDeSP"].ToString());

        public static bool ProcessarInscricoesForaRedeNasRodadas = bool.Parse(ConfigurationManager.AppSettings["ProcessarInscricoesForaRedeNasRodadas"].ToString());
        public static bool ProcessarDeslocamentoNasRodadas = bool.Parse(ConfigurationManager.AppSettings["ProcessarDeslocamentoNasRodadas"].ToString());
        public static int PadreTicaoEscola = int.Parse(ConfigurationManager.AppSettings["PadreTicaoEscola"].ToString());
        public static int PadreTicaoUnidade = int.Parse(ConfigurationManager.AppSettings["PadreTicaoUnidade"].ToString());

        public static bool simular = bool.Parse(ConfigurationManager.AppSettings["Simular"].ToString());
        public static string EscolasNaoCompatibiliza = ConfigurationManager.AppSettings["EscolasNaoCompatibiliza"].ToString();
        public static string AlunosTeste = ConfigurationManager.AppSettings["AlunosTeste"].ToString();
        public static string MotivoFase8 = ConfigurationManager.AppSettings["MotivoFase8"].ToString();
        public static bool InativarInteresseRematricula = bool.Parse(ConfigurationManager.AppSettings["InativarInteresseRematricula"].ToString());
        public static bool gerarCBI = bool.Parse(ConfigurationManager.AppSettings["gerarCBI"].ToString());
        public static bool EnviarEmail = bool.Parse(ConfigurationManager.AppSettings["EnviarEmail"].ToString());

        public static bool ConversaoAbandono = bool.Parse(ConfigurationManager.AppSettings["ConversaoAbandono"].ToString());

        public static StringBuilder sLog = new StringBuilder();

        public static readonly System.Text.RegularExpressions.Regex regmail = new System.Text.RegularExpressions.Regex(@"^[-A-Z0-9~!$%^&*_=+}{\'?]+(\.[-A-Z0-9~!$%^&*_=+}{\'?]+)*@([A-Z0-9_][-A-Z0-9_]*(\.[-A-Z0-9_]+)*\.(AERO|ARPA|BIZ|COM|COOP|EDU|GOV|INFO|INT|MIL|MUSEUM|NAME|NET|ORG|PRO|TRAVEL|MOBI|[A-Z][A-Z])|([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}))(:[0-9]{1,5})?$");
        private struct InicioFim
        {
            public readonly int Inicio, Fim;

            public InicioFim(int inicio, int fim)
            {
                Inicio = inicio;
                Fim = fim;
            }
        }

        //Diretorios
        public static string dir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        public static string diretorioRelatorio = Path.Combine(dir, DateTime.Now.ToString("yyyyMMdd"));
        public static string ArquivoRelatorio = System.IO.Path.Combine(diretorioRelatorio, "relatorio.");

        public static string ArquivoUnidades = System.IO.Path.Combine(dir, "unidades.bin");
        public static string ArquivoTurmas = System.IO.Path.Combine(dir, "turmas.bin");
        public static string ArquivoAlunosFicha = System.IO.Path.Combine(dir, "alunos.");
        public static string ArquivoIrmaos = System.IO.Path.Combine(dir, "irmaos.bin");

        public static string ArquivoPadreTicao = System.IO.Path.Combine(dir, "in.padreticao.txt");
        public static string ArquivoCEU = System.IO.Path.Combine(dir, "in.ceu.txt");

        public static Dictionary<int, Aluno> alunos;


        public static void Msg(string msg)
        {
            string mensagem = $"{msg} - {DateTime.Now.ToShortDateString()} @ {DateTime.Now.ToLongTimeString()}";
            sLog.AppendLine(mensagem);
            Console.WriteLine(mensagem);
        }

        private static void SerializarAlunos()
        {
            string arquivoAtual = ArquivoAlunosFicha + DateTime.UtcNow.ToBinary().ToString().PadLeft(20, '0') + ".bin";

            // Grava todos os alunos e as unidades já atribuídas a ele (não importa a ordem)
            using (System.IO.FileStream fileStream = new System.IO.FileStream(arquivoAtual, System.IO.FileMode.Create, System.IO.FileAccess.Write))
            {
                using (System.IO.BufferedStream bufferedStream = new System.IO.BufferedStream(fileStream, 32 * 1024))
                {
                    using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(bufferedStream, Encoding.UTF8, false))
                    {
                        writer.Write(0);
                        int total = alunos.Count;
                        writer.Write(total);
                        foreach (Aluno aluno in alunos.Values)
                            aluno.Serializar(writer);
                    }
                }
            }
        }

        private static Aluno SubstituiAlunoSeNecessario(Aluno alunoNovo)
        {
            Aluno alunoAntigo;

            if (alunos.TryGetValue(alunoNovo.Codigo, out alunoAntigo))
            {
                // Verifica se é necessário atualizar esse aluno
                if (
                    // Definições/inscrições têm preferência sobre continuidade
                    (!alunoNovo.Continuidade && alunoAntigo.Continuidade) ||
                    (!alunoNovo.Continuidade && !alunoAntigo.Continuidade && (
                        // Fichas novas têm preferência sobre fichas antigas
                        alunoNovo.CodigoFicha > alunoAntigo.CodigoFicha ||
                        // Se o aluno novo teve sua coordenada alterada, ele deve ser utilizado no lugar do antigo
                        Math.Abs(alunoNovo.Coordenada.Latitude - alunoAntigo.Coordenada.Latitude) > DeltaLatLng ||
                        Math.Abs(alunoNovo.Coordenada.Longitude - alunoAntigo.Coordenada.Longitude) > DeltaLatLng
                    )) ||
                    (alunoNovo.Continuidade && alunoAntigo.Continuidade && (
                        // Ocorreu alguma alteração cadastral
                        alunoNovo.TipoEnsinoDesejado != alunoAntigo.TipoEnsinoDesejado ||
                        alunoNovo.SerieDesejada != alunoAntigo.SerieDesejada ||
                        alunoNovo.TurnoDesejado != alunoAntigo.TurnoDesejado ||
                        alunoNovo.InteresseIntegral != alunoAntigo.InteresseIntegral
                    ))
                )
                {
                    // Passa a utilizar o aluno novo (alguma coisa mudou)
                    return alunoNovo;
                }

                // Pode continuar utilizando o aluno antigo
                return alunoAntigo;
            }

            // Esse aluno era novo, mesmo
            return alunoNovo;
        }

        private static void CarregarVagasMunicipais(IDataBase db, Dictionary<int, Unidade> unidades)
        {
            Msg("Carregando vagas do municipio a partir do banco");

            int total = 0, nao = 0, unidadeAtual = 0;
            using (SedDataReader reader = db.ExecuteSedReaderCommandText(Query.QueryVagasMunicipioSP))
            {
                while (reader.Read())
                {
                    int codigoEscola = reader.GetInt32NonDBNull(0),
                        tipoEnsino = reader.GetInt32NonDBNull(1),
                        serie = reader.GetInt32NonDBNull(2),
                        codigoUnidade = reader.GetInt32NonDBNull(3),
                        vagas = reader.GetInt32NonDBNull(4);

                    Unidade unidade;
                    total++;
                    if (!unidades.TryGetValue(Unidade.CorrigirCodigoUnidade(codigoEscola, codigoUnidade), out unidade))
                    {
                        nao++;
                        continue;
                    }
                    if (unidadeAtual == 0 || unidadeAtual != unidade.CodigoUnidadeCorrigido)
                    {
                        unidadeAtual = unidade.CodigoUnidadeCorrigido;
                        unidade.ConsiderarApenasVagasDaUnidade = false;
                    }
                    unidade.DefinirVagasMunicipais(tipoEnsino, serie, AlocarAlunosNasVagasMunicipaisDeSP ? vagas : 0);
                }
            }
            Msg($"{nao} de {total} turmas municipais nao encontradas");
        }

        private static bool CarregarUnidadesEAlunos(IDataBase db, Dictionary<int, Unidade> unidades, bool utilizarArquivos = true, bool rodadas = false)
        {
            Rodadas = rodadas;
            #region Leitura dos arquivos
            bool arquivosOk = false;

            if (utilizarArquivos)
            {
                if (System.IO.File.Exists(ArquivoUnidades))
                {
                    Msg("Recuperando unidades do arquivo");
                    // Lê todas as unidades do arquivo (não importa a ordem)
                    using (System.IO.FileStream fileStream = new System.IO.FileStream(ArquivoUnidades, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                    {
                        using (System.IO.BufferedStream bufferedStream = new System.IO.BufferedStream(fileStream, 32 * 1024))
                        {
                            using (System.IO.BinaryReader reader = new System.IO.BinaryReader(bufferedStream, Encoding.UTF8, false))
                            {
                                int total = reader.ReadInt32();
                                for (int i = 0; i < total; i++)
                                {
                                    Unidade unidade = Unidade.Deserializar(reader);
                                    unidades[unidade.CodigoUnidadeCorrigido] = unidade;
                                }
                            }
                        }
                    }

                    if (System.IO.File.Exists(ArquivoTurmas))
                    {
                        Msg("Recuperando turmas do arquivo");
                        // Lê todas as turmas do arquivo (não importa a ordem)
                        using (System.IO.FileStream fileStream = new System.IO.FileStream(ArquivoTurmas, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                        {
                            using (System.IO.BufferedStream bufferedStream = new System.IO.BufferedStream(fileStream, 32 * 1024))
                            {
                                using (System.IO.BinaryReader reader = new System.IO.BinaryReader(bufferedStream, Encoding.UTF8, false))
                                {
                                    int total = reader.ReadInt32();
                                    for (int i = 0; i < total; i++)
                                        Unidade.Turma.Deserializar(reader, unidades);
                                }
                            }
                        }

                        //Apenas para ajustar as vagas do municipio
                        //como no arquivo serealizado é gravado a capacidade maxima de cada turma
                        //quando o relatorio de vagas vem diferente da turma, é preciso reprocessar essas vagas
                        //para que não haja turmas a mais no cadastro de turmas do que no arquivos
                        if (db != null)
                        {
                            CarregarVagasMunicipais(db, unidades);
                        }
                        else
                        {
                            using (IDataBase dbAux = FactoryDataBase.Create(ConnectionStringWrite))
                            {
                                dbAux.SetCommandTimeout(0);
                                CarregarVagasMunicipais(dbAux, unidades);
                            }
                        }
                    }

                    // Procura pelo arquivo de alunos mais recente
                    string dir = System.IO.Path.GetDirectoryName(ArquivoAlunosFicha);
                    List<string> tmp = new List<string>(System.IO.Directory.EnumerateFiles(dir, "alunos.*"));
                    tmp.Sort((a, b) => { return a.CompareTo(b); });

                    if (tmp.Count > 0)
                    {
                        Msg("Recuperando alunos do arquivo");

                        // Lê todos os alunos do último arquivo gravado (não importa a ordem)
                        using (System.IO.FileStream fileStream = new System.IO.FileStream(tmp[tmp.Count - 1], System.IO.FileMode.Open, System.IO.FileAccess.Read))
                        {
                            using (System.IO.BufferedStream bufferedStream = new System.IO.BufferedStream(fileStream, 32 * 1024))
                            {
                                using (System.IO.BinaryReader reader = new System.IO.BinaryReader(bufferedStream, Encoding.UTF8, false))
                                {
                                    // Usava antes, mas agora não tem serventia
                                    //int proximoAProcessar = reader.ReadInt32();
                                    reader.ReadInt32();

                                    int total = reader.ReadInt32();
                                    for (int i = 0; i < total; i++)
                                    {
                                        Aluno alunoNovo = Aluno.Deserializar(reader, unidades);
                                        // Procura por fichas duplicadas e ignora as mais antigas
                                        Aluno alunoAntigo;
                                        if (alunos.TryGetValue(alunoNovo.Codigo, out alunoAntigo) &&
                                            alunoAntigo.CodigoFicha > alunoNovo.CodigoFicha)
                                            continue;
                                        alunos[alunoNovo.Codigo] = alunoNovo;
                                    }
                                }
                            }
                        }

                        if (rodadas)
                        {
                            arquivosOk = true;
                        }
                        else //if (System.IO.File.Exists(ArquivoIrmaos)) @@@ Não está mais utilizando arquivos para irmãos, sempre vai no banco buscar
                        {
                            Msg("Recuperando irmaos");

                            if (db != null)
                            {
                                CarregaDadosIrmaosSemRodada(db, unidades);
                            }
                            else
                            {
                                using (IDataBase dbAux = FactoryDataBase.Create(ConnectionStringWrite))
                                {
                                    dbAux.SetCommandTimeout(0);
                                    CarregaDadosIrmaosSemRodada(dbAux, unidades);
                                }
                            }
                            arquivosOk = true;
                        }
                    }
                }
            }
            else
            {
                arquivosOk = (db != null);
            }
            #endregion

            if (db == null)
                return arquivosOk;

            Msg("Inicio Compatibilização");
            if (!simular) db.ExecuteNonQueryCommandText(Query.QueryInicioCompatibiarra);

            //Antes de buscar as turmas faz os calculos das quantidades.
            #region Atualização Quantidades Alunos Turma
            Program.Msg($"Atualizando quantidades nas turmas");
            if (!simular)
            {
                db.ExecuteNonQueryCommandText(Query.QueryAtualizaTurmaQtds);
                db.ExecuteNonQueryCommandText(Query.GuardaPosicaoVagas);
            }
            #endregion

            #region Unidades e turmas

            Msg("Populando unidades no banco");
            // Cria a tabela temporária com as escolas/unidades de onde tiraremos as fichas
            db.ExecuteNonQueryCommandText(Query.QueryPrepararUnidades);

            // Limpa as turmas para atualizar todas do banco
            foreach (Unidade unidade in unidades.Values)
                unidade.LimparTiposEnsinoETurmas();

            Msg("Recuperando unidades e turmas do banco");

            HashSet<int> unidadesComGeoAlterada = new HashSet<int>();
            HashSet<int> unidadesComGeoJaValidada = new HashSet<int>();

            // Carrega as unidades/turmas do banco (atualizando os objetos locais)
            using (SedDataReader reader = db.ExecuteSedReaderCommandText(Query.QueryTurmas))
            {
                while (reader.Read())
                {
                    int CD_ESCOLA = reader.GetInt32(0),
                        CD_UNIDADE = Unidade.CorrigirCodigoUnidade(CD_ESCOLA, reader.GetInt32(1)),
                        CD_DIRETORIA_ESTADUAL,
                        CD_REDE_ENSINO,                      
                        CD_DNE;
                    //string DS_LATITUDE,
                    //	DS_LONGITUDE;
                    int CD_TIPO_ENSINO,
                        NR_SERIE,
                        CD_TURMA,
                        CD_DURACAO,
                        CD_TIPO_CLASSE,
                        CD_TURNO;
                    string ID_TURMA,
                        NR_SALA,
                        DS_TURMA;
                    int ACESSIVEL,
                        ID_CAPA_FISICA_MAX;
                    string NM_COMPLETO_ESCOLA,
                        NM_DIRETORIA,
                        NM_MUNICIPIO;
                    DateTime DT_INIC_AULA,
                        DT_FIM_AULA;
                    int NR_CLASSE,
                        ALUNOS_MATRICULADOS,
                        MAIOR_NR_ALUNO,
                        CD_STATUS_FLUXO_APROVACAO_TURMA;

                    Unidade unidade;
                    unidades.TryGetValue(CD_UNIDADE, out unidade);
                    if (!unidadesComGeoJaValidada.Contains(CD_UNIDADE))
                    {
                        unidadesComGeoJaValidada.Add(CD_UNIDADE);

                        double lat, lng;

                        if (reader.IsDBNull(5) || reader.IsDBNull(6) ||
                            (lat = reader.GetDouble(5)) >= 0 ||
                            (lng = reader.GetDouble(6)) >= 0)
                        {
                            lat = double.PositiveInfinity;
                            lng = double.PositiveInfinity;
                        }

                        // Atualização e não criação (apenas para validar a geo)
                        if (unidade != null)
                        {
                            if (double.IsInfinity(unidade.Coordenada.Latitude) == double.IsInfinity(lat))
                            {
                                if (unidade.CoordenadaValida)
                                {
                                    // Verifica se houve uma alteração na geolocalização
                                    if (Math.Abs(unidade.Coordenada.Latitude - lat) > DeltaLatLng ||
                                        Math.Abs(unidade.Coordenada.Longitude - lng) > DeltaLatLng)
                                    {
                                        unidadesComGeoAlterada.Add(CD_UNIDADE);
                                        unidade = null;
                                    }
                                }
                            }
                            else
                            {
                                // Ou essa unidade perdeu a geolocalização, ou acabou de ser geolocalizada
                                unidadesComGeoAlterada.Add(CD_UNIDADE);
                                unidade = null;
                            }
                        }

                        if (unidade == null)
                        {
                            // Unidade nova (ou com geo alterada)

                            CD_DIRETORIA_ESTADUAL = reader.GetInt32(2);
                            CD_REDE_ENSINO = reader.GetInt32(3);
                            CD_DNE = reader.GetInt32(4);
                            ACESSIVEL = reader.GetInt32(16);
                            NM_COMPLETO_ESCOLA = reader.GetStringNonDBNull(18, "").Trim();
                            NM_DIRETORIA = reader.GetStringNonDBNull(19, "").Trim();
                            NM_MUNICIPIO = reader.GetStringNonDBNull(20, "").Trim();

                            unidade = new Unidade(CD_ESCOLA, CD_UNIDADE, CD_DIRETORIA_ESTADUAL, CD_DNE, CD_REDE_ENSINO, NM_COMPLETO_ESCOLA, NM_DIRETORIA, NM_MUNICIPIO, ACESSIVEL != 0, lat, lng);

                            unidades[CD_UNIDADE] = unidade;
                        }
                    }

                    CD_TIPO_ENSINO = reader.GetInt32NonDBNull(7);
                    NR_SERIE = reader.GetInt32NonDBNull(8);
                    CD_TURMA = reader.GetInt32NonDBNull(9);
                    CD_DURACAO = reader.GetInt32NonDBNull(10);
                    CD_TIPO_CLASSE = reader.GetInt32NonDBNull(11);
                    CD_TURNO = reader.GetInt32NonDBNull(12);
                    ID_TURMA = reader.GetStringNonDBNull(13, "").Trim().ToUpper();
                    NR_SALA = reader.GetStringNonDBNull(14, "").Trim().ToUpper();
                    DS_TURMA = reader.GetStringNonDBNull(15, "").Trim().ToUpper();
                    ID_CAPA_FISICA_MAX = reader.GetInt32NonDBNull(17);
                    DT_INIC_AULA = reader.GetDateTimeNonDBNull(21);
                    DT_FIM_AULA = reader.GetDateTimeNonDBNull(22);
                    NR_CLASSE = reader.GetInt32NonDBNull(23);
                    ALUNOS_MATRICULADOS = reader.GetInt32NonDBNull(24);
                    MAIOR_NR_ALUNO = reader.GetInt32NonDBNull(25);
                    //Se não encontra o status da turma, deixa como homologada.
                    CD_STATUS_FLUXO_APROVACAO_TURMA = reader.GetInt16NonDBNull(26,2);

                    unidade.AdicionarTurma(CD_TIPO_ENSINO, NR_SERIE, CD_TURMA, CD_DURACAO, CD_TIPO_CLASSE, CD_TURNO, ID_CAPA_FISICA_MAX, ID_TURMA, NR_SALA, DS_TURMA, DT_INIC_AULA, DT_FIM_AULA, NR_CLASSE, ALUNOS_MATRICULADOS, MAIOR_NR_ALUNO, CD_STATUS_FLUXO_APROVACAO_TURMA);
                }
            }

            CarregarVagasMunicipais(db, unidades);

            if (utilizarArquivos)
            {
                Msg($"Gravando arquivo de unidades com {unidades.Count} unidades");
                // Grava todas as unidades no arquivo (não importa a ordem)
                int totalTurmas = 0;
                using (System.IO.FileStream fileStream = new System.IO.FileStream(ArquivoUnidades, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    using (System.IO.BufferedStream bufferedStream = new System.IO.BufferedStream(fileStream, 32 * 1024))
                    {
                        using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(bufferedStream, Encoding.UTF8, false))
                        {
                            writer.Write(unidades.Count);
                            foreach (Unidade unidade in unidades.Values)
                            {
                                totalTurmas += unidade.QuantidadeTurmas;
                                unidade.Serializar(writer);
                            }
                        }
                    }
                }

                Msg($"Gravando arquivo de turmas com {totalTurmas} turmas");
                // Grava todas as turmas no arquivo (não importa a ordem)
                using (System.IO.FileStream fileStream = new System.IO.FileStream(ArquivoTurmas, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    using (System.IO.BufferedStream bufferedStream = new System.IO.BufferedStream(fileStream, 32 * 1024))
                    {
                        using (System.IO.BinaryWriter writer = new System.IO.BinaryWriter(bufferedStream, Encoding.UTF8, false))
                        {
                            writer.Write(totalTurmas);
                            foreach (Unidade unidade in unidades.Values)
                                unidade.SerializarTurmas(writer);
                        }
                    }
                }

                if (unidadesComGeoAlterada.Count > 0)
                {
                    Msg($"*** {unidadesComGeoAlterada.Count} unidades tiveram sua geolocalizacao alterada, excluindo as rotas dos alunos");
                    foreach (Aluno aluno in alunos.Values)
                        aluno.InvalidarRotas(unidadesComGeoAlterada);
                    SerializarAlunos();

                    Msg($"*** Recarregando os arquivos");
                    unidadesComGeoAlterada.Clear();
                    unidadesComGeoJaValidada.Clear();
                    unidadesComGeoAlterada = null;
                    unidadesComGeoJaValidada = null;
                    unidades.Clear();
                    alunos.Clear();
                    GC.Collect();
                    return CarregarUnidadesEAlunos(db, unidades);
                }
            }
            #endregion

            if (rodadas)
            {
                #region Alunos Rodadas
                int ordem = 0;
                for (int r = 1; r <= 2; r++)
                {
                    int antes = alunos.Count;
                    Msg($"Recuperando alunos de inscricao do banco (etapa {r})");
                    using (SedDataReader reader = db.ExecuteSedReaderCommandText(r == 1 ? Query.QueryAlunosRodadasEtapa1 : Query.QueryAlunosRodadasEtapa2))
                    {
                        while (reader.Read())
                        {
                            int escolaDeOrigem = reader.GetInt32NonDBNull(13);
                            int unidadeDeOrigem = reader.GetInt32NonDBNull(14);

                            double lat, lng;

                            // * Se for fase 0, 8 ou 9, não deve utilizar as coordenadas da escola!
                            Aluno.DecifrarLatLng(r == 1, (reader.GetDecimalNonDBNull(9) + "").Replace(",", "."), (reader.GetDecimalNonDBNull(10) + "").Replace(",", "."),
                                unidades, escolaDeOrigem, unidadeDeOrigem, out lat, out lng);

                            Aluno aluno = Aluno.AlunoDeDefinicaoInscricao(
                                unidades,
                                reader.GetInt32(0),
                                reader.GetInt32(1),
                                reader.GetInt32NonDBNull(17),
                                reader.GetStringNonDBNull(15, "").Trim(),
                                reader.GetStringNonDBNull(16, "").Trim(),
                                reader.GetByteNonDBNull(8) != 0,
                                reader.GetDecimalNonDBNull(5) != 0,
                                reader.GetDecimalNonDBNull(6) != 0,
                                reader.GetDecimalNonDBNull(7) != 0,
                                (int)reader.GetDecimalNonDBNull(2),
                                (int)reader.GetDecimalNonDBNull(3),
                                (int)reader.GetDecimalNonDBNull(4),
                                lat,
                                lng,
                                escolaDeOrigem,
                                unidadeDeOrigem,
                                r == 1 ? reader.GetInt32NonDBNull(18) : reader.GetInt32NonDBNull(21),
                                r == 1 ? reader.GetInt64NonDBNull(19) : reader.GetInt64NonDBNull(22),
                                r == 1 ? (int)reader.GetDecimalNonDBNull(20) : (int)reader.GetDecimalNonDBNull(23),
                                0,
                                0,
                                0

                                );

                            aluno.RodadasEtapa1 = (r == 1);
                            aluno.Ordem = ++ordem;

                            // Reaproveita os dados de rotas já existentes, que foram previamente serializados
                            Aluno alunoExistente;
                            if (alunos.TryGetValue(aluno.Codigo, out alunoExistente))
                            {
                                // Caso bizarro de algum aluno que foi trazido do banco mais de uma vez
                                if (alunoExistente.Ordem > 0)
                                    continue;
                                // alunoExistente era um aluno previamente serializados
                                aluno.UnidadesCompatibilizadas.CopiarDe(alunoExistente.UnidadesCompatibilizadas);
                            }

                            if (r == 2)
                            {
                                aluno.DeslocamentoPorEndereco = (aluno.CodigoMotivoFase8 == 1);
                                aluno.CodigoMatriculaAnterior_089 = reader.GetInt64NonDBNull(18);
                                aluno.EscolaAnterior_089 = reader.GetInt32NonDBNull(19);
                                aluno.UnidadeAnteriorCorrigido_089 = Unidade.CorrigirCodigoUnidade(aluno.EscolaAnterior_089, reader.GetInt32NonDBNull(20));
                            }

                            alunos[aluno.Codigo] = aluno;
                        }



                        Msg($"{(alunos.Count - antes)} alunos obtidos na etapa {r}");
                    }
                }

                // Remove os alunos que tinham sido serializados mas não devem mais ser compatibilizados
                List<Aluno> alunosQueSairam = new List<Aluno>(10000);
                foreach (Aluno aluno in alunos.Values)
                {
                    // Ordem == 0 significa que o aluno não veio do banco, foi apenas previamente serializado
                    if (aluno.Ordem == 0)
                        alunosQueSairam.Add(aluno);

                    // Aproveita já para limpar os irmãos, para depois atualizar todos do banco
                    aluno.LimparIrmaos();
                }
                foreach (Aluno aluno in alunosQueSairam)
                    alunos.Remove(aluno.Codigo);
                alunosQueSairam.Clear();
                alunosQueSairam = null;

                // Agora traz e conecta todos os irmãos do banco
                Msg("Recuperando irmaos do banco");
                Dictionary<KeyValuePair<int, int>, KeyValuePair<Aluno, int>> pendentes = new Dictionary<KeyValuePair<int, int>, KeyValuePair<Aluno, int>>();
                Dictionary<int, Aluno> alunosFakesFaltantes = new Dictionary<int, Aluno>();

                using (SedDataReader reader = db.ExecuteSedReaderCommandText(Query.QueryIrmaos))
                {
                    while (reader.Read())
                    {
                        int i1 = reader.GetInt32(0), i2 = reader.GetInt32(1);
                        bool gemeo = reader.GetBoolean(2);
                        switch (Aluno.AssociarIrmaos(alunos, i1, i2, gemeo))
                        {
                            case 0:
                                // Nenhum aluno encontrado
                                continue;
                            case -1:
                                // Aluno i1 não encontrado
                                pendentes[new KeyValuePair<int, int>(Math.Min(i1, i2), Math.Max(i1, i2))] = new KeyValuePair<Aluno, int>(alunos[i2], gemeo ? -i1 : i1);
                                alunosFakesFaltantes[i1] = null;
                                break;
                            case -2:
                                // Aluno i2 não encontrado
                                pendentes[new KeyValuePair<int, int>(Math.Min(i1, i2), Math.Max(i1, i2))] = new KeyValuePair<Aluno, int>(alunos[i1], gemeo ? -i2 : i2);
                                alunosFakesFaltantes[i2] = null;
                                break;
                        }
                    }
                }

                Msg($"Ajustando {pendentes.Count} pares de irmaos de outras rodadas ({alunosFakesFaltantes.Count} alunos faltantes)");
                Dictionary<int, string> ids = new Dictionary<int, string>();
                StringBuilder sbIds = new StringBuilder();

                List<string> listaAlunos = new List<string>();
                int qtdeAluno = 0;
                foreach (int i in alunosFakesFaltantes.Keys)
                {
                    if (sbIds.Length > 0)
                        sbIds.Append(',');
                    sbIds.Append(i);
                    qtdeAluno += 1;
                    if (qtdeAluno >= 10000)
                    {
                        listaAlunos.Add(sbIds.ToString());
                        qtdeAluno = 0;
                        sbIds.Clear();
                    }
                }

                if (sbIds.Length > 0) listaAlunos.Add(sbIds.ToString());

                for (int i = 0; i < listaAlunos.Count; i++)
                {
                    using (SedDataReader reader = db.ExecuteSedReaderCommandText(Query.QueryTurmasIrmaosRodadas + listaAlunos[i] + ")"))
                    {
                        while (reader.Read())
                        {
                            int CD_ALUNO = reader.GetInt32NonDBNull(0),
                                CD_ESCOLA = reader.GetInt32NonDBNull(1),
                                CD_UNIDADE = reader.GetInt32NonDBNull(2),
                                CD_TURMA = reader.GetInt32NonDBNull(3),
                                ID_DEFIC = reader.GetByteNonDBNull(4);

                            Unidade unidade;
                            if (!unidades.TryGetValue(Unidade.CorrigirCodigoUnidade(CD_ESCOLA, CD_UNIDADE), out unidade))
                                continue;

                            Unidade.Turma turma;
                            if (!unidade.Turmas.TryGetValue(CD_TURMA, out turma))
                                continue;

                            alunosFakesFaltantes[CD_ALUNO] = Aluno.IrmaoFakeDeContinuidade(unidades, CD_ALUNO, "", "", ID_DEFIC != 0, turma.TipoEnsino, turma.Serie, CD_ESCOLA, CD_UNIDADE, turma.CodigoTurma, turma.Turno, turma.Duracao, turma.IdTurma, turma.Sala, unidade, turma);
                            break;
                        }
                    }
                }

                foreach (KeyValuePair<Aluno, int> p in pendentes.Values)
                {
                    Aluno irmaoFake;
                    if (alunosFakesFaltantes.TryGetValue(Math.Abs(p.Value), out irmaoFake) && irmaoFake != null)
                        irmaoFake.AdicionarIrmaoFake(p.Key, p.Value < 0);
                }
                pendentes.Clear();
                pendentes = null;
                alunosFakesFaltantes.Clear();
                alunosFakesFaltantes = null;

                Aluno[] alunosArray = new Aluno[alunos.Count];
                alunos.Values.CopyTo(alunosArray, 0);

                //DescobrirEscolasProximas(unidades, alunosArray);

                //CalcularRotas(alunos, false);
                #endregion
            }
            else
            {
                #region Alunos
                Msg("Recuperando alunos de definicao/inscricao do banco");
                // Atualiza os dados dos alunos/fichas do banco (definições e inscrições)
                Dictionary<int, Aluno> alunosAtualizados = new Dictionary<int, Aluno>(alunos.Count);
                using (SedDataReader reader = db.ExecuteSedReaderCommandText(Query.QueryAlunosCalculo2))
                {
                    while (reader.Read())
                    {
                        int escolaDeOrigem = reader.GetInt32NonDBNull(13);
                        int unidadeDeOrigem = reader.GetInt32NonDBNull(14);

                        double lat, lng;

                        // Antigamente os alunos sem geo válida eram descartados. Agora eles prosseguem,
                        // apenas para serem listados ao final do processo como Motivo.SemGeo
                        // 17/12/2019 - RAPHAEL Lopes
                        // remove os endereços indicativos reader.GetStringNonDBNull(11), reader.GetStringNonDBNull(12)
                        Aluno.DecifrarLatLng(true, (reader.GetDecimalNonDBNull(9) + "").Replace(",", "."), (reader.GetDecimalNonDBNull(10) + "").Replace(",", "."),
                                unidades, escolaDeOrigem, unidadeDeOrigem, out lat, out lng);

                        Aluno aluno = SubstituiAlunoSeNecessario(Aluno.AlunoDeDefinicaoInscricao(
                            unidades,
                            reader.GetInt32(0),
                            reader.GetInt32(1),
                            reader.GetInt32NonDBNull(17),
                            reader.GetStringNonDBNull(15, "").Trim(),
                            reader.GetStringNonDBNull(16, "").Trim(),
                            reader.GetByteNonDBNull(8) != 0,
                            reader.GetDecimalNonDBNull(5) != 0,
                            reader.GetDecimalNonDBNull(6) != 0,
                            reader.GetDecimalNonDBNull(7) != 0,
                            (int)reader.GetDecimalNonDBNull(2),
                            (int)reader.GetDecimalNonDBNull(3),
                            (int)reader.GetDecimalNonDBNull(4),
                            lat,
                            lng,
                            escolaDeOrigem,
                            unidadeDeOrigem,
                            reader.GetInt32NonDBNull(18),
                            reader.GetInt64NonDBNull(19),
                            (int)reader.GetDecimalNonDBNull(20),
                            reader.GetInt64NonDBNull(21),
                            reader.GetInt32NonDBNull(22),
                            reader.GetInt32NonDBNull(23)

                    ));

                        // Faz isso para manter os dados atualizados, e tudo funcionar bem com os
                        // alunos de continuidade, mas cria a cópia em alunosAtualizados por causa
                        // dos alunos/fichas que podem ter sido excluídos
                        alunos[aluno.Codigo] = aluno;
                        alunosAtualizados[aluno.Codigo] = aluno;
                        //Verifica se é deslocamento por endereço
                        aluno.DeslocamentoPorEndereco = (aluno.CodigoMotivoFase8 == 1);
                        //Só é utilizado em deslocamento
                        aluno.CodigoMatriculaAnterior_089 = aluno.CodigoMatriculaAnterior;
                        aluno.EscolaAnterior_089 = aluno.CodigoEscolaAnterior;
                        aluno.UnidadeAnteriorCorrigido_089 = Unidade.CorrigirCodigoUnidade(aluno.EscolaAnterior_089, aluno.CodigoUnidadeAnterior);
                    }
                }

                Msg($"{(alunos.Count)} alunos obtidos.");

                // Alunos que precisarão ter as rotas calculadas depois
                Aluno[] alunosArray = new Aluno[alunosAtualizados.Count];
                alunosAtualizados.Values.CopyTo(alunosArray, 0);

                alunos.Clear();
                alunos = alunosAtualizados;
                alunosAtualizados = null;

                // Limpa os irmãos para atualizar todos do banco
                foreach (Aluno aluno in alunos.Values)
                    aluno.LimparIrmaos();

                CarregaDadosIrmaosSemRodada(db, unidades);

                //DescobrirEscolasProximas(unidades, alunosArray);
                #endregion
            }

            #region Gravação dos alunos
            if (utilizarArquivos)
            {
                // Cria um arquivo com os alunos e outro com os irmãos
                Msg($"Gravando arquivo de alunos com {alunos.Count} alunos");
                SerializarAlunos();
            }
            #endregion

            #region Limpeza
            Msg("Limpando dados temporarios");
            db.ExecuteNonQueryCommandText(Query.QueryLimpeza);

            #endregion

            return arquivosOk;
        }

        private static void CarregaDadosIrmaosSemRodada(IDataBase db, Dictionary<int, Unidade> unidades)
        {
            Msg("Recuperando irmaos do banco");
            Dictionary<KeyValuePair<int, int>, KeyValuePair<Aluno, int>> pendentes = new Dictionary<KeyValuePair<int, int>, KeyValuePair<Aluno, int>>();
            Dictionary<int, Aluno> alunosFakesFaltantes = new Dictionary<int, Aluno>();

            using (SedDataReader reader = db.ExecuteSedReaderCommandText(Query.QueryIrmaos))
            {
                while (reader.Read())
                {
                    int i1 = reader.GetInt32(0), i2 = reader.GetInt32(1);
                    bool gemeo = reader.GetBoolean(2);
                    switch (Aluno.AssociarIrmaos(alunos, i1, i2, gemeo))
                    {
                        case 0:
                            // Nenhum aluno encontrado
                            continue;
                        case -1:
                            // Aluno i1 não encontrado
                            pendentes[new KeyValuePair<int, int>(Math.Min(i1, i2), Math.Max(i1, i2))] = new KeyValuePair<Aluno, int>(alunos[i2], gemeo ? -i1 : i1);
                            alunosFakesFaltantes[i1] = null;
                            break;
                        case -2:
                            // Aluno i2 não encontrado
                            pendentes[new KeyValuePair<int, int>(Math.Min(i1, i2), Math.Max(i1, i2))] = new KeyValuePair<Aluno, int>(alunos[i1], gemeo ? -i2 : i2);
                            alunosFakesFaltantes[i2] = null;
                            break;
                    }
                }
            }

            Msg($"Ajustando {pendentes.Count} pares de irmaos de outras rodadas ({alunosFakesFaltantes.Count} alunos faltantes)");

            if (pendentes.Count > 0)
            {
                db.ExecuteNonQueryCommandText(Query.QueryPrepararUnidades);
                db.ExecuteNonQueryCommandText(Query.QueryAlunosCalculo2);
                using (SedDataReader reader = db.ExecuteSedReaderCommandText(Query.QueryIrmaosSemRodada))
                {
                    while (reader.Read())
                    {
                        int CD_ALUNO = reader.GetInt32NonDBNull(0),
                            CD_ESCOLA = reader.GetInt32NonDBNull(1),
                            CD_UNIDADE = reader.GetInt32NonDBNull(2),
                            CD_TURMA = reader.GetInt32NonDBNull(3),
                            ID_DEFIC = reader.GetByteNonDBNull(4);

                        Unidade unidade;
                        if (!unidades.TryGetValue(Unidade.CorrigirCodigoUnidade(CD_ESCOLA, CD_UNIDADE), out unidade))
                            continue;

                        Unidade.Turma turma;
                        if (!unidade.Turmas.TryGetValue(CD_TURMA, out turma))
                            continue;

                        Aluno alunoFakeFaltante;
                        if (!alunosFakesFaltantes.TryGetValue(CD_ALUNO, out alunoFakeFaltante))
                            continue;

                        alunosFakesFaltantes[CD_ALUNO] = Aluno.IrmaoFakeDeContinuidade(unidades, CD_ALUNO, "", "", ID_DEFIC != 0, turma.TipoEnsino, turma.Serie, CD_ESCOLA, CD_UNIDADE, turma.CodigoTurma, turma.Turno, turma.Duracao, turma.IdTurma, turma.Sala, unidade, turma);
                        break;
                    }
                }
            }
            foreach (KeyValuePair<Aluno, int> p in pendentes.Values)
            {
                Aluno irmaoFake;
                if (alunosFakesFaltantes.TryGetValue(Math.Abs(p.Value), out irmaoFake) && irmaoFake != null)
                    irmaoFake.AdicionarIrmaoFake(p.Key, p.Value < 0);
            }
            pendentes.Clear();
            pendentes = null;
            alunosFakesFaltantes.Clear();
            alunosFakesFaltantes = null;
        }

        private static void TransferenciaRPP(IDataBase db)
        {
            DateTime dataHoraCompat = DateTime.Now;
            int idRodada = (dataHoraCompat.Year * 10000) + (dataHoraCompat.Month * 100) + dataHoraCompat.Day;
            #region Atualização do Status dos Alunos
            Msg("Atualizando status dos alunos transferidos e inserindo novos registros");
            db.ClearParameters();

            db.ExecuteNonQueryCommandText($@"
                --GUARDA OS REGISTROS A MODIFICAR
                DROP TABLE IF EXISTS #TB_ALUNO_RPP_TMP
                SELECT DISTINCT RPP.*, I.CD_ESCOLA AS CD_ESCOLA_D, I.CD_DIRETORIA AS CD_DIRETORIA_D, I.CD_DNE AS CD_DNE_D, MA.DT_ANO_LETIVO AS DT_ANO_LETIVO_D, CAST(0 AS BIGINT) AS CD_MATRICULA_ALUNO_NOVO, CAST(0 AS INT) AS CD_TURMA_NOVA
                INTO #TB_ALUNO_RPP_TMP
                FROM DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA WITH(NOLOCK) 
                INNER JOIN DB_DIARIO_CLASSE.dbo.TB_ALUNO_RPP RPP WITH(NOLOCK) 
                    ON MA.CD_ALUNO = RPP.CD_ALUNO
                INNER JOIN CALCULO_ROTAS..TB_REL_COMPAT_TRANSFERENCIA_MOVIMENTACAO RL WITH(NOLOCK) 
                    ON MA.CD_MATRICULA_ALUNO = RL.CD_MATRICULA_ALUNO
                INNER JOIN CALCULO_ROTAS..TB_REL_COMPAT_REAL I WITH(NOLOCK) 
                    ON I.CD_ALUNO = MA.CD_ALUNO
					AND I.ID_RODADA = RL.ID_RODADA
                WHERE 
                    MA.FL_SITUACAO_ALUNO_CLASSE = 1
                    AND RPP.CD_ALUNO_STATUS_RPP IN (1, 3)
                    AND MA.LOGIN_ALTER = '[AUTO COMPAT DESLOC]'
                    AND RL.ID_RODADA = {idRodada}

                --DEFINE AS A MATRICULA NOVA DAS MATRICULAS CRIADAS
                UPDATE #TB_ALUNO_RPP_TMP
                SET CD_MATRICULA_ALUNO_NOVO = MA.CD_MATRICULA_ALUNO,
                    CD_TURMA_NOVA = MA.CD_TURMA
                FROM #TB_ALUNO_RPP_TMP RPP WITH(NOLOCK) 
                LEFT JOIN DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA WITH(NOLOCK) 
                    ON MA.CD_ALUNO=RPP.CD_ALUNO
                    AND MA.NR_GRAU = RPP.CD_TIPO_ENSINO
	                AND MA.FL_SITUACAO_ALUNO_CLASSE = 0
                    AND RPP.DT_ANO_LETIVO_D = MA.DT_ANO_LETIVO
                    AND MA.LOGIN_INCL IN ('[Compatibilizacao]')

                --COLOCA STATUS DE TRANSFERENCIA NOS REGISTROS DE TRANSFERENCIA
                UPDATE RPP 
                    SET RPP.CD_ALUNO_STATUS_RPP = 4
                       ,RPP.LOGIN_ALTER = 'Compatibilizacao'
                       ,RPP.DT_ALTER = GETDATE()
                FROM DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA WITH(NOLOCK) 
                INNER JOIN DB_DIARIO_CLASSE.dbo.TB_ALUNO_RPP RPP WITH(NOLOCK) 
                    ON MA.CD_ALUNO = RPP.CD_ALUNO
                INNER JOIN CALCULO_ROTAS..TB_REL_COMPAT_TRANSFERENCIA_MOVIMENTACAO RL WITH(NOLOCK) 
                    ON MA.CD_MATRICULA_ALUNO = RL.CD_MATRICULA_ALUNO
                INNER JOIN CALCULO_ROTAS..TB_REL_COMPAT_REAL I WITH(NOLOCK) 
                    ON I.CD_ALUNO = MA.CD_ALUNO
					AND I.ID_RODADA = RL.ID_RODADA
                WHERE 
                    FL_SITUACAO_ALUNO_CLASSE = 1
                    AND RPP.CD_ALUNO_STATUS_RPP IN (1, 3)
                    AND MA.LOGIN_ALTER = '[AUTO COMPAT DESLOC]'
                    AND I.ID_RODADA = {idRodada}

                --COLOCA STATUS DE TRANSFERENCIA NOS REGISTROS DE BAIXA POR TANSFERENCIA
                UPDATE RPP 
                    SET RPP.CD_ALUNO_STATUS_RPP = 4 
                FROM DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA WITH(NOLOCK) 
                INNER JOIN DB_DIARIO_CLASSE.dbo.TB_ALUNO_RPP RPP WITH(NOLOCK) 
                    ON MA.CD_ALUNO = RPP.CD_ALUNO
                INNER JOIN CALCULO_ROTAS..TB_REL_COMPAT_REAL I WITH(NOLOCK) 
                    ON I.CD_MATRICULA_ALUNO = MA.CD_MATRICULA_ALUNO
                WHERE 
                    MA.FL_SITUACAO_ALUNO_CLASSE = 31
                    AND RPP.CD_ALUNO_STATUS_RPP IN (1, 3)
	                AND MA.LOGIN_ALTER = '[AUTO COMPAT]'
                    AND ID_RODADA = {idRodada}



                --ADICIONAR AS MATRICULAS DOS ALUNOS TRANSFERIDOS
                INSERT INTO DB_DIARIO_CLASSE.dbo.TB_ALUNO_RPP 
                    (DT_ANO_LETIVO
                    ,CD_TIPO_ENSINO	
                    ,CD_MATRICULA_ALUNO	
                    ,CD_ALUNO	
                    ,CD_DIRETORIA	
                    ,CD_ESCOLA	
                    ,CD_TURMA	
                    ,CD_UNIDADE_CURRICULAR	
                    ,CD_DISCIPLINA	
                    ,CD_MUNICIPIO	
                    ,DT_INCLUSAO	
                    ,CD_USER_INCLUSAO	
                    ,DT_EXCLUSAO	
                    ,CD_USER_EXCLUSAO	
                    ,CD_ALUNO_STATUS_RPP
                    ,CD_MATRICULA_ANTERIOR
                    ,CD_MATRICULA_ATUAL)
                SELECT DISTINCT
                    RPP.DT_ANO_LETIVO_D,
                    RPP.CD_TIPO_ENSINO,
                    RPP.CD_MATRICULA_ALUNO,
                    RPP.CD_ALUNO,
                    RPP.CD_DIRETORIA_D,
                    RPP.CD_ESCOLA_D,
	                CASE WHEN MA.CD_TURMA IS NULL THEN 0 ELSE MA.CD_TURMA END,
                    RPP.CD_UNIDADE_CURRICULAR,
                    RPP.CD_DISCIPLINA,
                    RPP.CD_DNE_D,
                    GETDATE() AS DT_INCLUSAO,
                    0 AS CD_USER_INCLUSAO,
                    NULL AS DT_EXCLUSAO,
                    NULL AS CD_USER_EXCLUSAO,
                    1 AS CD_ALUNO_STATUS_RPP,
	                RPP.CD_MATRICULA_ALUNO AS CD_MATRICULA_ANTERIOR,
                    CASE WHEN MA.CD_MATRICULA_ALUNO IS NULL THEN 0 ELSE MA.CD_MATRICULA_ALUNO END
                FROM #TB_ALUNO_RPP_TMP RPP WITH(NOLOCK) 
				LEFT JOIN DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA WITH(NOLOCK) 
                    ON MA.CD_ALUNO=RPP.CD_ALUNO
                    AND MA.NR_GRAU = RPP.CD_TIPO_ENSINO
					AND MA.FL_SITUACAO_ALUNO_CLASSE = 0
                    AND RPP.DT_ANO_LETIVO_D = MA.DT_ANO_LETIVO
	                AND MA.LOGIN_INCL IN ('[Compatibilizacao]')
				");
            db.ClearParameters();
            #endregion
        }
        //#32605 - Transferencia de notas
        private static void TransferenciaNotas(IDataBase db)
        {
            DateTime dataHoraCompat = DateTime.Now;
            int idRodada = (dataHoraCompat.Year * 10000) + (dataHoraCompat.Month * 100) + dataHoraCompat.Day;
            db.ClearParameters();

            db.ExecuteNonQueryCommandText($@"
               DROP TABLE IF EXISTS #TB_ALUNO_NOTA_TMP

                SELECT DISTINCT MA_NOVA.CD_MATRICULA_ALUNO_ANTERIOR, MA_NOVA.CD_MATRICULA_ALUNO, MA.DT_ANO_LETIVO
                INTO #TB_ALUNO_NOTA_TMP
                FROM DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA WITH(NOLOCK) 
                INNER JOIN DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA_NOVA WITH(NOLOCK) ON MA.CD_MATRICULA_ALUNO=MA_NOVA.CD_MATRICULA_ALUNO_ANTERIOR
                AND MA.DT_ANO_LETIVO=MA_NOVA.DT_ANO_LETIVO
                INNER JOIN CALCULO_ROTAS..TB_REL_COMPAT_TRANSFERENCIA_MOVIMENTACAO RL WITH(NOLOCK) 
                    ON MA.CD_MATRICULA_ALUNO = RL.CD_MATRICULA_ALUNO
                WHERE 
                    MA.FL_SITUACAO_ALUNO_CLASSE = 1
	                AND MA_NOVA.FL_SITUACAO_ALUNO_CLASSE = 0
                    AND MA.LOGIN_ALTER = '[AUTO COMPAT DESLOC]'
                    AND RL.ID_RODADA = {idRodada}
	                AND MA.DT_ANO_LETIVO='{AnoLetivoStr}'
	                AND MA_NOVA.DT_ANO_LETIVO='{AnoLetivoStr}'

                DECLARE @MA_ORIGEM BIGINT
                DECLARE @MA_DESTINO BIGINT
                DECLARE @ANO_LETIVO INT
 
                DECLARE db_cursor CURSOR FOR SELECT CD_MATRICULA_ALUNO_ANTERIOR,CD_MATRICULA_ALUNO, DT_ANO_LETIVO FROM #TB_ALUNO_NOTA_TMP
                OPEN db_cursor;
                FETCH NEXT FROM db_cursor INTO @MA_ORIGEM, @MA_DESTINO, @ANO_LETIVO;
                WHILE @@FETCH_STATUS = 0  
                BEGIN  

		                EXEC [DB_DIARIO_CLASSE].dbo.PROC_FECHAMENTO_TRANFERENCIA_NOTAS @MA_ORIGEM, @MA_DESTINO, @ANO_LETIVO

		                FETCH NEXT FROM db_cursor INTO @MA_ORIGEM, @MA_DESTINO, @ANO_LETIVO;
                END;
                CLOSE db_cursor;
                DEALLOCATE db_cursor;
				");
            db.ClearParameters();
        }

        private static void LogFichasForaDoParametro(IDataBase db)
        {
            DateTime dataHoraCompat = DateTime.Now;
            int idRodada = (dataHoraCompat.Year * 10000) + (dataHoraCompat.Month * 100) + dataHoraCompat.Day;

            db.ClearParameters();

            List<string> fases = new List<string>();

            if (Program.ProcessarDeslocamentoNasRodadas)
                fases.Add("0, 8, 9");
            if (Program.ProcessarInscricoesForaRedeNasRodadas)
                fases.Add("4,7");

            string comando = $@"
                UPDATE FI SET SITUACAO_COMP_DEF='FORA DO PARAMETRO DA COMPATIBILIZAÇÃO'
                FROM CADALUNOS..TB_FICHA_INSCRICAO FI WITH(NOLOCK) 
				LEFT JOIN CALCULO_ROTAS..TB_REL_COMPAT_REAL I (NOLOCK) ON FI.ID_ALUNO=I.CD_ALUNO AND FI.ID_FICHA_INSCRICAO=I.ID_FICHA AND I.ID_RODADA='{idRodada}'
                WHERE
	                FI.ANO_LETIVO = '{Program.AnoLetivo}' 
                AND (FI.ID_ALUNO IN ({Program.AlunosTeste}) OR '{Program.AlunosTeste}' = '0')
                AND ISNULL(FI.FL_SIT_INSCR, 0) = 0
                AND ISNULL(FI.CD_MOTIVO_CANCEL, 0) = 0
                {(fases.Count > 0 ? $@"AND FI.FL_FASE IN ({String.Join(",", fases)})" : "AND FI.FL_FASE = 99999")}
                AND ISNULL(FI.CD_ESCOL_COMP_DEF, 0) = 0
                AND FI.ID_GRAU IN (2, 6, 14, 78, 80, 81, 82, 83, 84, 101)
				AND I.ID_REL_COMPAT_REAL IS NULL
				AND FI.SITUACAO_COMP_DEF IS NULL";

            db.ExecuteNonQueryCommandText(comando);
            db.ClearParameters();
        }
        private static void AlocarAlunosIgnoraEscolha(List<Ordem> OrdemExecucao, Dictionary<int, Unidade> unidades, bool simular, bool rodadas = false)
        {
            foreach (Aluno aluno in alunos.Values)
            {
                if (!aluno.InscricaoDeslocamentoSemEndereco)
                    aluno.InteresseIntegral = !aluno.InteresseIntegral;
            }
            AlocarAlunos(OrdemExecucao, unidades, simular, true, rodadas);
        }
        private static void AlocarAlunos(List<Ordem> OrdemExecucao, Dictionary<int, Unidade> unidades, bool simular, bool UltimaTentativaAlocacao, bool rodadas = false)
        {
            Rodadas = rodadas;
            // ***** Irmãos, se tiver 3 ou mais coloca todos juntos (mesmo que fique distante)
            //
            // A ordem das alocações: (MUITO IMPORTANTE!)
            // 1 - Definição CEU e Padre Ticão  (algoritmo 1) - VAI CONTINUAR MESMA COISA
            // 2 - Continuidade (algoritmo 0) - NÂO VAI USAR AQUI
            // 3 - Definição-Continuidade regular (algoritmo 4) - NÂO VAI USAR AQUI
            // 4 - Definição de deficientes integral (algoritmo 3)
            // 5 - Inscrição de deficientes integral (algoritmo 3)
            // 6 - Definição regular integral (algoritmo 3)
            // 7 - Inscrição regular integral (algoritmo 3)
            // 8 - Definição de deficientes com qualquer tipo de irmão (algoritmo 2)
            // 9 - Definição de deficientes (algoritmo 3)
            // 10 - Inscrição de deficientes com qualquer tipo de irmão (algoritmo 2)
            // 11 - Inscrição de deficientes (algoritmo 3)
            // 12 - Definição de gêmeos (algoritmo 2)
            // 13 - Definição de irmãos (algoritmo 2)
            // 14 - Inscrição de gêmeos (algoritmo 2)
            // 15 - Inscrição de irmãos (algoritmo 2)
            // 16 - Definição regular (algoritmo 3)
            // 17 - Inscrição regular (algoritmo 3)
            // 18 - Inscrição Sem Alteração de Endereço (algoritmo 3)

            StringBuilder builder = new StringBuilder(1024 * 1024);
            List<Aluno.Irmao> irmaos = new List<Aluno.Irmao>(500000);
            List<Aluno> alunosCongelados = new List<Aluno>(100000);
            List<Aluno> alunosPadreTicaoPendentes = new List<Aluno>();
            List<Aluno> alunosOrdenados;

            #region Remoção de Alunos do Município de SP
            Msg("Removendo alunos invalidos do municipio");
            // Devemos ignorar as alocações de alunos de continuidade das unidades da rede municipal de SP
            // (Para a capital, 6o ano é continuidade, e eles nem deveriam aparecer aqui)
            alunosOrdenados = new List<Aluno>(2000000);
            foreach (Aluno aluno in alunos.Values)
            {
                if (aluno.UnidadeDeOrigem != null &&
                    aluno.UnidadeDeOrigem.CodigoMunicipio == 9668 &&
                    aluno.UnidadeDeOrigem.CodigoRedeEnsino == 2 &&
                    (aluno.Continuidade || (aluno.Definicao && aluno.SerieDesejada == 6)))
                    alunosOrdenados.Add(aluno);
            }
            // Remove todos esses alunos antes de prosseguir
            for (int i = alunosOrdenados.Count - 1; i >= 0; i--)
                alunos.Remove(alunosOrdenados[i].Codigo);
            alunosOrdenados.Clear();
            alunosOrdenados = null;
            #endregion

            GC.Collect();

            Msg("Coletando pares de irmaos");
            foreach (Aluno aluno in alunos.Values)
                aluno.ColetarParesDeIrmaos(irmaos);

            if (!rodadas)
            {
                #region Remoção de Alunos
                if (!UltimaTentativaAlocacao)
                {
                    Msg("Removendo alunos");
                    using (IDataBase db = FactoryDataBase.Create(ConnectionStringRead))
                    {
                        db.SetCommandTimeout(0);

                        Msg("Preparando turmas para remocao");
                        db.ExecuteNonQueryCommandText(Query.QueryPrepararUnidades);
                        db.ExecuteNonQueryCommandText(Query.QueryAlunosContinuidadePre);

                        Msg("Contando alunos de continuidade");
                        using (var reader = db.ExecuteSedReaderCommandText(Query.QueryAlunosRestantesContinuidade))
                        {
                            while (reader.Read())
                            {
                                Aluno aluno;
                                if (!alunos.TryGetValue(reader.GetInt32(0), out aluno))
                                    continue;
                                aluno.Permaneceu = true;
                            }
                        }

                        Msg("Contando alunos de definicao/inscricao");
                        using (var reader = db.ExecuteSedReaderCommandText(Query.QueryAlunosRestantesDefinicaoInscricao))
                        {
                            while (reader.Read())
                            {
                                Aluno aluno;
                                if (!alunos.TryGetValue(reader.GetInt32(0), out aluno))
                                    continue;
                                aluno.Permaneceu = true;
                            }
                        }
                    }
                    foreach (Aluno aluno in alunos.Values)
                    {
                        if (aluno.Permaneceu)
                            continue;
                        alunosCongelados.Add(aluno);
                    }
                    Msg($"Removendo {alunosCongelados.Count} alunos");
                    for (int i = alunosCongelados.Count - 1; i >= 0; i--)
                        alunos.Remove(alunosCongelados[i].Codigo);
                    alunosCongelados.Clear();
                }
                #endregion

                #region Congelamento de Dados
                /*
                Msg("Congelando alunos");
                using (IDataBase db = FactoryDataBase.Create(ConnectionStringRead))
                {
                    db.SetCommandTimeout(0);

                    using (var reader = db.ExecuteSedReaderCommandText(Query.QueryAlunosMatriculadosManual))
                    {
                        while (reader.Read())
                        {
                            int CD_ALUNO = reader.GetInt32(0),
                                CD_TURMA = reader.GetInt32(1),
                                CD_ESCOLA = reader.GetInt32(2),
                                CD_UNIDADE = Unidade.CorrigirCodigoUnidade(CD_ESCOLA, reader.GetInt32(3));

                            Aluno aluno;
                            if (!alunos.TryGetValue(CD_ALUNO, out aluno))
                                continue;

                            alunos.Remove(aluno.Codigo);
                            aluno.Congelado = true;
                            alunosCongelados.Add(aluno);

                            Unidade unidade;
                            if (!unidades.TryGetValue(CD_UNIDADE, out unidade))
                                continue;

                            if (unidade.ConsiderarApenasVagasDaUnidade)
                            {
                                unidade.TentarAlocarAlunoApenasPorVagas(aluno, false, Motivo.Congelamento);
                                aluno.Motivo = Motivo.Congelamento;
                                aluno.UnidadeAlocada = unidade;
                                continue;
                            }

                            Unidade.Turma turma;
                            if (!unidade.Turmas.TryGetValue(CD_TURMA, out turma))
                                continue;

                            turma.Alunos[aluno.Codigo] = aluno;
                            aluno.Motivo = Motivo.Congelamento;
                            aluno.TurmaAlocada = turma;
                            aluno.UnidadeAlocada = unidade;
                        }
                    }
                }
                Msg($"{alunosCongelados.Count} alunos congelados com sucesso");
                */
                #endregion

                //************PADRE TICAO E CEU PRECISA DO ARQUIVO DO MENDES******************
                #region Padre Ticão e CEU
                Msg("Distribuindo alunos por RA");

                Dictionary<string, Aluno> alunosPorRA = new Dictionary<string, Aluno>(alunos.Count);
                foreach (Aluno aluno in alunos.Values)
                    alunosPorRA[aluno.RA] = aluno;

                Msg("Alocando alunos Padre Ticao");

                builder.Clear();

                Unidade padreTicao;
                if (unidades.TryGetValue(PadreTicaoUnidade, out padreTicao) && System.IO.File.Exists(ArquivoPadreTicao))
                {
                    int alunosTotais = 0, alunosFora = 0;
                    foreach (string ra in System.IO.File.ReadAllLines(ArquivoPadreTicao))
                    {
                        if (string.IsNullOrWhiteSpace(ra))
                            continue;
                        alunosTotais++;
                        Aluno aluno;
                        if (alunosPorRA.TryGetValue(Aluno.NormalizarRA(ra), out aluno))
                        {
                            aluno.TentarSeAlocarEmAlgumaUnidadeCompatibilizadaPadreTicao(rodadas);
                            if (!aluno.JaAlocado)
                            {
                                alunos.Remove(aluno.Codigo);
                                alunosPadreTicaoPendentes.Add(aluno);
                            }
                        }
                        else
                        {
                            builder.AppendLine($"Aluno sem geo ou sem ficha de inscrição\t{ra}\t");
                            alunosFora++;
                        }
                    }
                    Msg($"{alunosFora} de {alunosTotais} alunos da Padre Ticao nao foram encontrados");
                }
                else
                {
                    Msg("Unidade Padre Ticao e/ou seu arquivo de alunos nao foi(ram) encontrado(s)");
                }
                if (builder.Length > 0)
                    System.IO.File.WriteAllText(ArquivoPadreTicao + ".naoencontrado.txt", builder.ToString(), Encoding.UTF8);

                Msg("Ordenando alunos");

                GC.Collect();

                #region CEU
                Msg("Alocando alunos CEU");

                builder.Clear();

                Dictionary<int, List<Aluno>> alunosCEU = new Dictionary<int, List<Aluno>>();

                if (System.IO.File.Exists(ArquivoCEU))
                {
                    int alunosTotais = 0, alunosFora = 0;
                    foreach (string linha in System.IO.File.ReadAllLines(ArquivoCEU))
                    {
                        if (string.IsNullOrWhiteSpace(linha))
                            continue;
                        string[] partes = linha.Split('\t');
                        if (partes.Length != 3)
                            continue;
                        //RA Escola Unidade
                        int escola, unidadeOriginal, unidadeCorrigida;
                        if (!int.TryParse(partes[1], out escola) ||
                            !int.TryParse(partes[2], out unidadeOriginal))
                            continue;

                        alunosTotais++;

                        unidadeCorrigida = Unidade.CorrigirCodigoUnidade(escola, unidadeOriginal);

                        Unidade unidade;
                        if (!unidades.TryGetValue(unidadeCorrigida, out unidade))
                        {
                            builder.AppendLine($"Unidade não encontrada\t{escola}\t{unidadeOriginal}");
                            continue;
                        }

                        string ra;
                        Aluno aluno;
                        if (alunosPorRA.TryGetValue(ra = Aluno.NormalizarRA(partes[0]), out aluno))
                        {
                            if (aluno.JaAlocado)
                            {
                                // Apareceu tanto no Padre Ticão como aqui!?!?!?
                                builder.AppendLine($"Alocado na Padre Ticão\t{ra}\t");
                            }
                            else
                            {
                                List<Aluno> alunosNesseCEU;
                                if (!alunosCEU.TryGetValue(unidadeCorrigida, out alunosNesseCEU))
                                {
                                    alunosNesseCEU = new List<Aluno>();
                                    alunosCEU[unidadeCorrigida] = alunosNesseCEU;
                                }
                                alunosNesseCEU.Add(aluno);
                            }
                        }
                        else
                        {
                            builder.AppendLine($"Aluno sem geo ou sem ficha de definição/inscrição\t{ra}\t");
                            alunosFora++;
                        }
                    }

                    //Ordena os alunos por distância e depois tenta matricular nessa ordem
                    foreach (KeyValuePair<int, List<Aluno>> par in alunosCEU)
                    {
                        Unidade unidade = unidades[par.Key];
                        par.Value.Sort((a, b) =>
                        {
                            // Deficientes devem aparecer antes
                            if (a.Deficiente != b.Deficiente)
                                return (a.Deficiente ? -1 : 1);
                            // O caso dos irmãos está tratado automaticamente, pois para ser irmão,
                            // a distância deve ser igual/muito próxima, logo, eles apareceriam em
                            // sequência depois da ordenação
                            double da = a.DistanciaAteUnidade(unidade);
                            double db = b.DistanciaAteUnidade(unidade);
                            if (da > 999999)
                            {
                                if (db > 999999)
                                    return 0;
                                return 1;
                            }
                            else if (db > 999999)
                            {
                                return -1;
                            }
                            else
                            {
                                if (da > db)
                                    return 1;
                                return (da < db ? -1 : 0);
                            }
                        });
                        foreach (Aluno aluno in par.Value)
                        {
                            // Apareceu duas vezes na listagem dos CEUs
                            if (aluno.JaAlocado)
                                continue;

                            if (!unidade.TentarAlocarAlunoCEU(aluno))
                            {
                                aluno.Motivo = Motivo.SemVagasCEU;
                                builder.AppendLine($"CEU sem vagas\t{Aluno.NormalizarRA(aluno.RA)}\t");
                                alunosFora++;
                            }
                        }
                    }

                    Msg($"{alunosFora} de {alunosTotais} nao foram alocados em CEUs pois o aluno ou a unidade nao foi(ram) localizado(s) ou nao havia vaga");
                }
                else
                {
                    Msg("O arquivo de CEU nao foi encontrado");
                }
                if (builder.Length > 0)
                    System.IO.File.WriteAllText(ArquivoCEU + ".naoencontrado.txt", builder.ToString(), Encoding.UTF8);

                alunosPorRA.Clear();
                alunosPorRA = null;

                #endregion

                GC.Collect();
                #endregion
            }

            List<Aluno> lideres = new List<Aluno>(16);
            List<Aluno> seguidores = new List<Aluno>(16);
            List<Unidade> unidadesTemp = new List<Unidade>(16);
            List<int> turnosTemp = new List<int>(16);

            bool etapa1 = true;
            do
            {
                if (rodadas)
                    Msg($"**** Iniciando etapa {(etapa1 ? 1 : 2)} de 2 ****");

                #region Definição-Continuidade regular
                Msg("Alocando alunos (Definicao-Continuidade regular)");
                ///primeiro vai tentar fazer contnuidade
                ///caso não encontre vaga na continuidade esse aluno tentará alocar de outra forma
                ///utilizando a rota a pé
                foreach (Ordem ordem in OrdemExecucao.Where(w => w.continuidade))
                {
                    Aluno aluno;
                    alunos.TryGetValue(ordem.codigoAluno, out aluno);

                    if (aluno == null)
                    {
                        ordem.MotivoStr = "Aluno Sem Ficha de Inscrição - continuidade";
                        continue;
                    }

                    if ((rodadas && aluno.RodadasEtapa1 != etapa1) || aluno.JaAlocado || !aluno.Definicao) //|| aluno.Continuidade 
                        continue;

                    if (aluno.UnidadeDeOrigem != null)
                        aluno.TentarSeAlocarNaUnidadeDeOrigem(rodadas);

                }
                #endregion

                #region Definição de deficientes integral
                Msg("Alocando alunos (Definicao de deficientes integral)");
                foreach (Ordem ordem in OrdemExecucao)
                {
                    Aluno aluno;
                    alunos.TryGetValue(ordem.codigoAluno, out aluno);

                    if (aluno == null)
                    {
                        ordem.MotivoStr = "Aluno Sem Ficha de Inscrição";
                        continue;
                    }

                    if ((rodadas && aluno.RodadasEtapa1 != etapa1) || aluno.JaAlocado || aluno.Continuidade || !aluno.Definicao || aluno.InscricaoDeslocamentoSemEndereco || !aluno.Deficiente || !aluno.InteresseIntegral)
                        continue;


                    Unidade unidadeCompat;
                    var CodigoUnidadeCorrigido = Unidade.CorrigirCodigoUnidade(ordem.codigoEscola, ordem.codigoUnidade);
                    unidades.TryGetValue(CodigoUnidadeCorrigido, out unidadeCompat);
                    if (unidadeCompat == null)
                    {
                        ordem.MotivoStr = "Sem Unidade Compatibilizada";
                        aluno.Motivo = Motivo.SemUnidadesCompatibilizadas;
                        continue;
                    }
                    aluno.CodigoEndereco = ordem.codigoEnderecoAluno;
                    aluno.AdicionarUnidadeCompatibilizada(unidadeCompat, ordem.distancia);
                    aluno.TentarSeAlocarEmAlgumaUnidadeCompatibilizadaIntegral(rodadas);
                    ordem.MotivoStr = aluno.MotivoStr;
                }
                #endregion

                #region Inscrição de deficientes integral
                Msg("Alocando alunos (Inscricao de deficientes integral)");
                foreach (Ordem ordem in OrdemExecucao)
                {
                    Aluno aluno;
                    alunos.TryGetValue(ordem.codigoAluno, out aluno);

                    if (aluno == null)
                    {
                        ordem.MotivoStr = "Aluno Sem Ficha de Inscrição";
                        continue;
                    }

                    if ((rodadas && aluno.RodadasEtapa1 != etapa1) || aluno.JaAlocado || aluno.Continuidade || aluno.Definicao || aluno.InscricaoDeslocamentoSemEndereco || !aluno.Deficiente || !aluno.InteresseIntegral)
                        continue;

                    Unidade unidadeCompat;
                    var CodigoUnidadeCorrigido = Unidade.CorrigirCodigoUnidade(ordem.codigoEscola, ordem.codigoUnidade);
                    unidades.TryGetValue(CodigoUnidadeCorrigido, out unidadeCompat);
                    if (unidadeCompat == null)
                    {
                        ordem.MotivoStr = "Sem Unidade Compatibilizada";
                        aluno.Motivo = Motivo.SemUnidadesCompatibilizadas;
                        continue;
                    }
                    aluno.CodigoEndereco = ordem.codigoEnderecoAluno;
                    aluno.AdicionarUnidadeCompatibilizada(unidadeCompat, ordem.distancia);
                    aluno.TentarSeAlocarEmAlgumaUnidadeCompatibilizadaIntegral(rodadas);
                    ordem.MotivoStr = aluno.MotivoStr;
                }
                #endregion

                #region Definição regular integral
                Msg("Alocando alunos (Definicao regular integral)");
                foreach (Ordem ordem in OrdemExecucao)
                {
                    Aluno aluno;
                    alunos.TryGetValue(ordem.codigoAluno, out aluno);

                    if (aluno == null)
                    {
                        ordem.MotivoStr = "Aluno Sem Ficha de Inscrição";
                        continue;
                    }

                    if ((rodadas && aluno.RodadasEtapa1 != etapa1) || aluno.JaAlocado || aluno.Continuidade || !aluno.Definicao || aluno.InscricaoDeslocamentoSemEndereco || !aluno.InteresseIntegral)
                        continue;

                    Unidade unidadeCompat;
                    var CodigoUnidadeCorrigido = Unidade.CorrigirCodigoUnidade(ordem.codigoEscola, ordem.codigoUnidade);
                    unidades.TryGetValue(CodigoUnidadeCorrigido, out unidadeCompat);
                    if (unidadeCompat == null)
                    {
                        ordem.MotivoStr = "Sem Unidade Compatibilizada";
                        aluno.Motivo = Motivo.SemUnidadesCompatibilizadas;
                        continue;
                    }
                    aluno.CodigoEndereco = ordem.codigoEnderecoAluno;
                    aluno.AdicionarUnidadeCompatibilizada(unidadeCompat, ordem.distancia);
                    aluno.TentarSeAlocarEmAlgumaUnidadeCompatibilizadaIntegral(rodadas);
                    ordem.MotivoStr = aluno.MotivoStr;
                }
                #endregion

                #region Inscrição regular integral
                Msg("Alocando alunos (Inscricao regular integral)");
                foreach (Ordem ordem in OrdemExecucao)
                {
                    Aluno aluno;
                    alunos.TryGetValue(ordem.codigoAluno, out aluno);

                    if (aluno == null)
                    {
                        ordem.MotivoStr = "Aluno Sem Ficha de Inscrição";
                        continue;
                    }

                    if ((rodadas && aluno.RodadasEtapa1 != etapa1) || aluno.JaAlocado || aluno.Continuidade || aluno.Definicao || aluno.InscricaoDeslocamentoSemEndereco || !aluno.InteresseIntegral)
                        continue;

                    Unidade unidadeCompat;
                    var CodigoUnidadeCorrigido = Unidade.CorrigirCodigoUnidade(ordem.codigoEscola, ordem.codigoUnidade);
                    unidades.TryGetValue(CodigoUnidadeCorrigido, out unidadeCompat);
                    if (unidadeCompat == null)
                    {
                        ordem.MotivoStr = "Sem Unidade Compatibilizada";
                        aluno.Motivo = Motivo.SemUnidadesCompatibilizadas;
                        continue;
                    }
                    aluno.CodigoEndereco = ordem.codigoEnderecoAluno;
                    aluno.AdicionarUnidadeCompatibilizada(unidadeCompat, ordem.distancia);
                    aluno.TentarSeAlocarEmAlgumaUnidadeCompatibilizadaIntegral(rodadas);
                    ordem.MotivoStr = aluno.MotivoStr;
                }
                #endregion

                #region Definição de deficientes com qualquer tipo de irmão
                Msg("Alocando alunos (Definicao de deficientes com qualquer tipo de irmao)");
                foreach (Ordem ordem in OrdemExecucao)
                {
                    Aluno aluno;
                    alunos.TryGetValue(ordem.codigoAluno, out aluno);
                    if (aluno == null)
                    {
                        ordem.MotivoStr = "Aluno Sem Ficha de Inscrição";
                        continue;
                    }

                    if (!aluno.PossuiIrmao) continue;

                    foreach (Aluno.Irmao irmao in irmaos)
                    {
                        if (aluno.Equals(irmao.Aluno1))
                        {
                            // Se apenas um aluno for deficiente, será o Aluno1
                            if ((rodadas && aluno.RodadasEtapa1 != etapa1) || aluno.JaAlocado || aluno.Continuidade || !aluno.Definicao || aluno.InscricaoDeslocamentoSemEndereco || !aluno.Deficiente)
                                continue;

                            Unidade unidadeCompat;
                            var CodigoUnidadeCorrigido = Unidade.CorrigirCodigoUnidade(ordem.codigoEscola, ordem.codigoUnidade);
                            unidades.TryGetValue(CodigoUnidadeCorrigido, out unidadeCompat);
                            if (unidadeCompat == null)
                            {
                                ordem.MotivoStr = "Sem Unidade Compatibilizada";
                                aluno.Motivo = Motivo.SemUnidadesCompatibilizadas;
                                continue;
                            }

                            aluno.CodigoEndereco = ordem.codigoEnderecoAluno;
                            aluno.AdicionarUnidadeCompatibilizada(unidadeCompat, ordem.distancia);
                            aluno.TentarSeAlocarJuntoComIrmaosEmAlgumaUnidadeCompatibilizada(rodadas, lideres, seguidores, unidadesTemp, turnosTemp);
                            ordem.MotivoStr = aluno.MotivoStr;
                        }
                    }
                }
                #endregion

                #region Definição de deficientes
                Msg("Alocando alunos (Definicao de deficientes)");
                foreach (Ordem ordem in OrdemExecucao)
                {
                    Aluno aluno;
                    alunos.TryGetValue(ordem.codigoAluno, out aluno);

                    if (aluno == null)
                    {
                        ordem.MotivoStr = "Aluno Sem Ficha de Inscrição";
                        continue;
                    }


                    if ((rodadas && aluno.RodadasEtapa1 != etapa1) || aluno.JaAlocado || aluno.Continuidade || !aluno.Definicao || aluno.InscricaoDeslocamentoSemEndereco || !aluno.Deficiente)
                        continue;

                    Unidade unidadeCompat;
                    var CodigoUnidadeCorrigido = Unidade.CorrigirCodigoUnidade(ordem.codigoEscola, ordem.codigoUnidade);
                    unidades.TryGetValue(CodigoUnidadeCorrigido, out unidadeCompat);
                    if (unidadeCompat == null)
                    {
                        ordem.MotivoStr = "Sem Unidade Compatibilizada";
                        aluno.Motivo = Motivo.SemUnidadesCompatibilizadas;
                        continue;
                    }
                    aluno.CodigoEndereco = ordem.codigoEnderecoAluno;
                    aluno.AdicionarUnidadeCompatibilizada(unidadeCompat, ordem.distancia);
                    aluno.TentarSeAlocarEmAlgumaUnidadeCompatibilizada(rodadas);
                    ordem.MotivoStr = aluno.MotivoStr;
                }
                #endregion

                #region Inscrição de deficientes com qualquer tipo de irmão
                Msg("Alocando alunos (Inscricao de deficientes com qualquer tipo de irmao)");

                foreach (Ordem ordem in OrdemExecucao)
                {
                    Aluno aluno;
                    alunos.TryGetValue(ordem.codigoAluno, out aluno);

                    if (aluno == null)
                    {
                        ordem.MotivoStr = "Aluno Sem Ficha de Inscrição";
                        continue;
                    }

                    if (!aluno.PossuiIrmao) continue;

                    foreach (Aluno.Irmao irmao in irmaos)
                    {
                        if (aluno.Equals(irmao.Aluno1))
                        {
                            // Se apenas um aluno for deficiente, será o Aluno1
                            if ((rodadas && aluno.RodadasEtapa1 != etapa1) || aluno.JaAlocado || aluno.Continuidade || aluno.Definicao || aluno.InscricaoDeslocamentoSemEndereco || !aluno.Deficiente)
                                continue;

                            Unidade unidadeCompat;
                            var CodigoUnidadeCorrigido = Unidade.CorrigirCodigoUnidade(ordem.codigoEscola, ordem.codigoUnidade);
                            unidades.TryGetValue(CodigoUnidadeCorrigido, out unidadeCompat);
                            if (unidadeCompat == null)
                            {
                                ordem.MotivoStr = "Sem Unidade Compatibilizada";
                                aluno.Motivo = Motivo.SemUnidadesCompatibilizadas;
                                continue;
                            }

                            aluno.CodigoEndereco = ordem.codigoEnderecoAluno;
                            aluno.AdicionarUnidadeCompatibilizada(unidadeCompat, ordem.distancia);
                            aluno.TentarSeAlocarJuntoComIrmaosEmAlgumaUnidadeCompatibilizada(rodadas, lideres, seguidores, unidadesTemp, turnosTemp);
                            ordem.MotivoStr = aluno.MotivoStr;
                        }
                    }
                }
                #endregion

                #region Inscrição de deficientes
                Msg("Alocando alunos (Inscricao de deficientes)");
                foreach (Ordem ordem in OrdemExecucao)
                {
                    Aluno aluno;
                    alunos.TryGetValue(ordem.codigoAluno, out aluno);

                    if (aluno == null)
                    {
                        ordem.MotivoStr = "Aluno Sem Ficha de Inscrição";
                        continue;
                    }

                    if ((rodadas && aluno.RodadasEtapa1 != etapa1) || aluno.JaAlocado || aluno.Continuidade || aluno.Definicao || aluno.InscricaoDeslocamentoSemEndereco || !aluno.Deficiente)
                        continue;

                    Unidade unidadeCompat;
                    var CodigoUnidadeCorrigido = Unidade.CorrigirCodigoUnidade(ordem.codigoEscola, ordem.codigoUnidade);
                    unidades.TryGetValue(CodigoUnidadeCorrigido, out unidadeCompat);
                    if (unidadeCompat == null)
                    {
                        ordem.MotivoStr = "Sem Unidade Compatibilizada";
                        aluno.Motivo = Motivo.SemUnidadesCompatibilizadas;
                        continue;
                    }
                    aluno.CodigoEndereco = ordem.codigoEnderecoAluno;
                    aluno.AdicionarUnidadeCompatibilizada(unidadeCompat, ordem.distancia);
                    aluno.TentarSeAlocarEmAlgumaUnidadeCompatibilizada(rodadas);
                    ordem.MotivoStr = aluno.MotivoStr;
                }
                #endregion

                #region Definição de gêmeos
                Msg("Alocando alunos (Definicao de gemeos)");
                foreach (Ordem ordem in OrdemExecucao)
                {
                    Aluno aluno;
                    alunos.TryGetValue(ordem.codigoAluno, out aluno);

                    if (aluno == null)
                    {
                        ordem.MotivoStr = "Aluno Sem Ficha de Inscrição";
                        continue;
                    }

                    if (!aluno.PossuiIrmao) continue;

                    foreach (Aluno.Irmao irmao in irmaos)
                    {
                        if (aluno.Equals(irmao.Aluno1))
                        {
                            // Se apenas um aluno for deficiente, será o Aluno1
                            if ((rodadas && aluno.RodadasEtapa1 != etapa1) || aluno.JaAlocado || aluno.Continuidade || !aluno.Definicao || aluno.InscricaoDeslocamentoSemEndereco || !aluno.Gemeo)
                                continue;

                            Unidade unidadeCompat;
                            var CodigoUnidadeCorrigido = Unidade.CorrigirCodigoUnidade(ordem.codigoEscola, ordem.codigoUnidade);
                            unidades.TryGetValue(CodigoUnidadeCorrigido, out unidadeCompat);
                            if (unidadeCompat == null)
                            {
                                ordem.MotivoStr = "Sem Unidade Compatibilizada";
                                aluno.Motivo = Motivo.SemUnidadesCompatibilizadas;
                                continue;
                            }
                            aluno.CodigoEndereco = ordem.codigoEnderecoAluno;
                            aluno.AdicionarUnidadeCompatibilizada(unidadeCompat, ordem.distancia);
                            aluno.TentarSeAlocarJuntoComIrmaosEmAlgumaUnidadeCompatibilizada(rodadas, lideres, seguidores, unidadesTemp, turnosTemp);
                            ordem.MotivoStr = aluno.MotivoStr;
                        }
                        else if (aluno.Equals(irmao.Aluno2))
                        {
                            // Se apenas um aluno for deficiente, será o Aluno1
                            if ((rodadas && aluno.RodadasEtapa1 != etapa1) || aluno.JaAlocado || aluno.Continuidade || !aluno.Definicao || aluno.InscricaoDeslocamentoSemEndereco || !aluno.Gemeo)
                                continue;

                            Unidade unidadeCompat;
                            var CodigoUnidadeCorrigido = Unidade.CorrigirCodigoUnidade(ordem.codigoEscola, ordem.codigoUnidade);
                            unidades.TryGetValue(CodigoUnidadeCorrigido, out unidadeCompat);
                            if (unidadeCompat == null)
                            {
                                ordem.MotivoStr = "Sem Unidade Compatibilizada";
                                aluno.Motivo = Motivo.SemUnidadesCompatibilizadas;
                                continue;
                            }
                            aluno.CodigoEndereco = ordem.codigoEnderecoAluno;
                            aluno.AdicionarUnidadeCompatibilizada(unidadeCompat, ordem.distancia);
                            aluno.TentarSeAlocarJuntoComIrmaosEmAlgumaUnidadeCompatibilizada(rodadas, lideres, seguidores, unidadesTemp, turnosTemp);
                            ordem.MotivoStr = aluno.MotivoStr;
                        }
                    }
                }
                #endregion

                #region Definição de irmãos
                Msg("Alocando alunos (Definicao de irmaos)");
                foreach (Ordem ordem in OrdemExecucao)
                {
                    Aluno aluno;
                    alunos.TryGetValue(ordem.codigoAluno, out aluno);

                    if (aluno == null)
                    {
                        ordem.MotivoStr = "Aluno Sem Ficha de Inscrição";
                        continue;
                    }

                    if (!aluno.PossuiIrmao) continue;

                    foreach (Aluno.Irmao irmao in irmaos)
                    {
                        if (aluno.Equals(irmao.Aluno1))
                        {
                            // Se apenas um aluno for deficiente, será o Aluno1
                            if ((rodadas && aluno.RodadasEtapa1 != etapa1) || aluno.JaAlocado || aluno.Continuidade || !aluno.Definicao || aluno.InscricaoDeslocamentoSemEndereco)
                                continue;

                            Unidade unidadeCompat;
                            var CodigoUnidadeCorrigido = Unidade.CorrigirCodigoUnidade(ordem.codigoEscola, ordem.codigoUnidade);
                            unidades.TryGetValue(CodigoUnidadeCorrigido, out unidadeCompat);
                            if (unidadeCompat == null)
                            {
                                ordem.MotivoStr = "Sem Unidade Compatibilizada";
                                aluno.Motivo = Motivo.SemUnidadesCompatibilizadas;
                                continue;
                            }
                            aluno.CodigoEndereco = ordem.codigoEnderecoAluno;
                            aluno.AdicionarUnidadeCompatibilizada(unidadeCompat, ordem.distancia);
                            aluno.TentarSeAlocarJuntoComIrmaosEmAlgumaUnidadeCompatibilizada(rodadas, lideres, seguidores, unidadesTemp, turnosTemp);
                            ordem.MotivoStr = aluno.MotivoStr;
                        }
                        else if (aluno.Equals(irmao.Aluno2))
                        {
                            // Se apenas um aluno for deficiente, será o Aluno1
                            if ((rodadas && aluno.RodadasEtapa1 != etapa1) || aluno.JaAlocado || aluno.Continuidade || !aluno.Definicao || aluno.InscricaoDeslocamentoSemEndereco)
                                continue;

                            Unidade unidadeCompat;
                            var CodigoUnidadeCorrigido = Unidade.CorrigirCodigoUnidade(ordem.codigoEscola, ordem.codigoUnidade);
                            unidades.TryGetValue(CodigoUnidadeCorrigido, out unidadeCompat);
                            if (unidadeCompat == null)
                            {
                                ordem.MotivoStr = "Sem Unidade Compatibilizada";
                                aluno.Motivo = Motivo.SemUnidadesCompatibilizadas;
                                continue;
                            }

                            aluno.CodigoEndereco = ordem.codigoEnderecoAluno;
                            aluno.AdicionarUnidadeCompatibilizada(unidadeCompat, ordem.distancia);
                            aluno.TentarSeAlocarJuntoComIrmaosEmAlgumaUnidadeCompatibilizada(rodadas, lideres, seguidores, unidadesTemp, turnosTemp);
                            ordem.MotivoStr = aluno.MotivoStr;
                        }
                    }
                }
                #endregion

                #region Inscrição de gêmeos
                Msg("Alocando alunos (Inscricao de gemeos)");
                foreach (Ordem ordem in OrdemExecucao)
                {
                    Aluno aluno;
                    alunos.TryGetValue(ordem.codigoAluno, out aluno);

                    if (aluno == null)
                    {
                        ordem.MotivoStr = "Aluno Sem Ficha de Inscrição";
                        continue;
                    }
                    if (!aluno.PossuiIrmao) continue;

                    foreach (Aluno.Irmao irmao in irmaos)
                    {
                        if (aluno.Equals(irmao.Aluno1))
                        {
                            // Se apenas um aluno for deficiente, será o Aluno1
                            if ((rodadas && aluno.RodadasEtapa1 != etapa1) || aluno.JaAlocado || aluno.Continuidade || aluno.Definicao || aluno.InscricaoDeslocamentoSemEndereco || !aluno.Gemeo)
                                continue;

                            Unidade unidadeCompat;
                            var CodigoUnidadeCorrigido = Unidade.CorrigirCodigoUnidade(ordem.codigoEscola, ordem.codigoUnidade);
                            unidades.TryGetValue(CodigoUnidadeCorrigido, out unidadeCompat);
                            if (unidadeCompat == null)
                            {
                                ordem.MotivoStr = "Sem Unidade Compatibilizada";
                                aluno.Motivo = Motivo.SemUnidadesCompatibilizadas;
                                continue;
                            }
                            aluno.CodigoEndereco = ordem.codigoEnderecoAluno;
                            aluno.AdicionarUnidadeCompatibilizada(unidadeCompat, ordem.distancia);
                            aluno.TentarSeAlocarJuntoComIrmaosEmAlgumaUnidadeCompatibilizada(rodadas, lideres, seguidores, unidadesTemp, turnosTemp);
                            ordem.MotivoStr = aluno.MotivoStr;
                        }
                        else if (aluno.Equals(irmao.Aluno2))
                        {
                            // Se apenas um aluno for deficiente, será o Aluno1
                            if ((rodadas && aluno.RodadasEtapa1 != etapa1) || aluno.JaAlocado || aluno.Continuidade || aluno.Definicao || aluno.InscricaoDeslocamentoSemEndereco || !aluno.Gemeo)
                                continue;

                            Unidade unidadeCompat;
                            var CodigoUnidadeCorrigido = Unidade.CorrigirCodigoUnidade(ordem.codigoEscola, ordem.codigoUnidade);
                            unidades.TryGetValue(CodigoUnidadeCorrigido, out unidadeCompat);
                            if (unidadeCompat == null)
                            {
                                ordem.MotivoStr = "Sem Unidade Compatibilizada";
                                aluno.Motivo = Motivo.SemUnidadesCompatibilizadas;
                                continue;
                            }
                            aluno.CodigoEndereco = ordem.codigoEnderecoAluno;
                            aluno.AdicionarUnidadeCompatibilizada(unidadeCompat, ordem.distancia);
                            aluno.TentarSeAlocarJuntoComIrmaosEmAlgumaUnidadeCompatibilizada(rodadas, lideres, seguidores, unidadesTemp, turnosTemp);
                            ordem.MotivoStr = aluno.MotivoStr;
                        }
                    }
                }

                #endregion

                #region Inscrição de irmãos
                Msg("Alocando alunos (Inscricao de irmaos)");
                foreach (Ordem ordem in OrdemExecucao)
                {
                    Aluno aluno;
                    alunos.TryGetValue(ordem.codigoAluno, out aluno);

                    if (aluno == null)
                    {
                        ordem.MotivoStr = "Aluno Sem Ficha de Inscrição";
                        continue;
                    }
                    if (!aluno.PossuiIrmao) continue;

                    foreach (Aluno.Irmao irmao in irmaos)
                    {
                        if (aluno.Equals(irmao.Aluno1))
                        {
                            // Se apenas um aluno for deficiente, será o Aluno1
                            if ((rodadas && aluno.RodadasEtapa1 != etapa1) || aluno.JaAlocado || aluno.Continuidade || aluno.Definicao || aluno.InscricaoDeslocamentoSemEndereco)
                                continue;

                            Unidade unidadeCompat;
                            var CodigoUnidadeCorrigido = Unidade.CorrigirCodigoUnidade(ordem.codigoEscola, ordem.codigoUnidade);
                            unidades.TryGetValue(CodigoUnidadeCorrigido, out unidadeCompat);
                            if (unidadeCompat == null)
                            {
                                ordem.MotivoStr = "Sem Unidade Compatibilizada";
                                aluno.Motivo = Motivo.SemUnidadesCompatibilizadas;
                                continue;
                            }
                            aluno.CodigoEndereco = ordem.codigoEnderecoAluno;
                            aluno.AdicionarUnidadeCompatibilizada(unidadeCompat, ordem.distancia);
                            aluno.TentarSeAlocarJuntoComIrmaosEmAlgumaUnidadeCompatibilizada(rodadas, lideres, seguidores, unidadesTemp, turnosTemp);
                            ordem.MotivoStr = aluno.MotivoStr;
                        }
                        else if (aluno.Equals(irmao.Aluno2))
                        {
                            // Se apenas um aluno for deficiente, será o Aluno1
                            if ((rodadas && aluno.RodadasEtapa1 != etapa1) || aluno.JaAlocado || aluno.Continuidade || aluno.Definicao || aluno.InscricaoDeslocamentoSemEndereco)
                                continue;

                            Unidade unidadeCompat;
                            var CodigoUnidadeCorrigido = Unidade.CorrigirCodigoUnidade(ordem.codigoEscola, ordem.codigoUnidade);
                            unidades.TryGetValue(CodigoUnidadeCorrigido, out unidadeCompat);
                            if (unidadeCompat == null)
                            {
                                ordem.MotivoStr = "Sem Unidade Compatibilizada";
                                aluno.Motivo = Motivo.SemUnidadesCompatibilizadas;
                                continue;
                            }
                            aluno.CodigoEndereco = ordem.codigoEnderecoAluno;
                            aluno.AdicionarUnidadeCompatibilizada(unidadeCompat, ordem.distancia);
                            aluno.TentarSeAlocarJuntoComIrmaosEmAlgumaUnidadeCompatibilizada(rodadas, lideres, seguidores, unidadesTemp, turnosTemp);
                            ordem.MotivoStr = aluno.MotivoStr;
                        }
                    }
                }
                #endregion

                #region Definição regular
                Msg("Alocando alunos (Definicao regular)");
                foreach (Ordem ordem in OrdemExecucao)
                {
                    Aluno aluno;
                    alunos.TryGetValue(ordem.codigoAluno, out aluno);

                    if (aluno == null)
                    {
                        ordem.MotivoStr = "Aluno Sem Ficha de Inscrição";
                        continue;
                    }

                    if ((rodadas && aluno.RodadasEtapa1 != etapa1) || aluno.JaAlocado || aluno.Continuidade || !aluno.Definicao || aluno.InscricaoDeslocamentoSemEndereco) // || aluno.InteresseIntegral)
                        continue;

                    Unidade unidadeCompat;
                    var CodigoUnidadeCorrigido = Unidade.CorrigirCodigoUnidade(ordem.codigoEscola, ordem.codigoUnidade);
                    unidades.TryGetValue(CodigoUnidadeCorrigido, out unidadeCompat);
                    if (unidadeCompat == null)
                    {
                        ordem.MotivoStr = "Sem Unidade Compatibilizada";
                        aluno.Motivo = Motivo.SemUnidadesCompatibilizadas;
                        continue;
                    }
                    aluno.CodigoEndereco = ordem.codigoEnderecoAluno;
                    aluno.AdicionarUnidadeCompatibilizada(unidadeCompat, ordem.distancia);
                    aluno.TentarSeAlocarEmAlgumaUnidadeCompatibilizada(rodadas);
                    ordem.MotivoStr = aluno.MotivoStr;
                }
                #endregion

                #region Inscrição regular
                Msg("Alocando alunos (Inscricao regular)");
                foreach (Ordem ordem in OrdemExecucao)
                {
                    Aluno aluno;
                    alunos.TryGetValue(ordem.codigoAluno, out aluno);

                    if (aluno == null)
                    {
                        ordem.MotivoStr = "Aluno Sem Ficha de Inscrição";
                        continue;
                    }

                    if ((rodadas && aluno.RodadasEtapa1 != etapa1) || aluno.JaAlocado || aluno.Continuidade || aluno.Definicao || aluno.InscricaoDeslocamentoSemEndereco) // || aluno.InteresseIntegral)
                        continue;

                    Unidade unidadeCompat;
                    var CodigoUnidadeCorrigido = Unidade.CorrigirCodigoUnidade(ordem.codigoEscola, ordem.codigoUnidade);
                    unidades.TryGetValue(CodigoUnidadeCorrigido, out unidadeCompat);
                    if (unidadeCompat == null)
                    {
                        ordem.MotivoStr = "Sem Unidade Compatibilizada";
                        aluno.Motivo = Motivo.SemUnidadesCompatibilizadas;
                        continue;
                    }
                    aluno.CodigoEndereco = ordem.codigoEnderecoAluno;
                    aluno.AdicionarUnidadeCompatibilizada(unidadeCompat, ordem.distancia);
                    aluno.TentarSeAlocarEmAlgumaUnidadeCompatibilizada(rodadas);
                    ordem.MotivoStr = aluno.MotivoStr;
                }
                #endregion

                #region Inscrição por deslocamento com alteração de endereço
                Msg("Alocando alunos (Inscricao por deslocamento com alteracao de endereco)");
                foreach (Aluno aluno in alunos.Values)
                {
                    if ((rodadas && aluno.RodadasEtapa1 != etapa1) || aluno.JaAlocado || aluno.Continuidade || aluno.Definicao || !aluno.InscricaoDeslocamentoComEndereco)
                        continue;
                    if (aluno.UnidadeDeOrigem != null)
                    {
                        aluno.TentarSeAlocarNaUnidadeDeOrigemComAlteracaoDeEndereco(rodadas);
                    }
                }
                #endregion

                #region Inscrição por deslocamento sem alteração de endereço
                Msg("Alocando alunos (Inscricao por deslocamento sem alteracao de endereco)");
                foreach (Aluno aluno in alunos.Values)
                {
                    if ((rodadas && aluno.RodadasEtapa1 != etapa1) || aluno.JaAlocado || aluno.Continuidade || aluno.Definicao || !aluno.InscricaoDeslocamentoSemEndereco)
                        continue;
                    if (aluno.UnidadeDeOrigem != null)
                    {
                        aluno.TentarSeAlocarNaUnidadeDeOrigem(rodadas);
                    }
                }
                #endregion

                etapa1 = !etapa1;
            } while (rodadas && !etapa1);

            if (UltimaTentativaAlocacao)
            {

                lideres.Clear();
                lideres = null;
                seguidores.Clear();
                seguidores = null;
                unidadesTemp.Clear();
                unidadesTemp = null;
                turnosTemp.Clear();
                turnosTemp = null;

                //Devolve os alunos pendentes da Padre Ticão para irem para o relatório
                foreach (Aluno aluno in alunosPadreTicaoPendentes)
                    alunos.Add(aluno.Codigo, aluno);

                alunosPadreTicaoPendentes.Clear();
                alunosPadreTicaoPendentes = null;

                //Restaura os alunos congelados para irem para o relatório
                foreach (Aluno aluno in alunosCongelados)
                    if (aluno.JaAlocado) alunos.Add(aluno.Codigo, aluno);

                alunosCongelados.Clear();
                alunosCongelados = null;

                GC.Collect();

                // Quando a compatibilização foi executada?
                DateTime dataHoraCompat = DateTime.Now;
                DateTime dataCompat = dataHoraCompat.Date;
                string dataCompatStrYMA = dataHoraCompat.ToString("yyyyMMdd");
                string dataCompatStrHora = dataHoraCompat.ToString("HHmmss");
                string dataCompatStrHora5 = dataHoraCompat.ToString("HH:mm");
                int idRodada = (dataHoraCompat.Year * 10000) + (dataHoraCompat.Month * 100) + dataHoraCompat.Day;

                Msg($"Data considerada da Compatibilizacao: {dataCompatStrYMA}");
                Msg($"Hora considerada da Compatibilizacao: {dataCompatStrHora}");

                #region Resultado da Simulação de Alocação

                //Cria o Diretorio Dos Relatorios
                if (!Directory.Exists(diretorioRelatorio)) Directory.CreateDirectory(diretorioRelatorio);


                Msg("Exportando dados das 15 escolas");
                builder.Clear();
                builder.AppendLine("Ficha\tCódigo Aluno\tRA\tNome\tRede\tCIE\tEscola\tDistância");
                foreach (Aluno aluno in alunos.Values)
                {
                    foreach (Aluno.UnidadeCompatibilizada unidade in aluno.UnidadesCompatibilizadas)
                    {
                        builder.Append(aluno.CodigoFicha);
                        builder.Append('\t');
                        builder.Append(aluno.Codigo);
                        builder.Append('\t');
                        builder.Append(aluno.RA);
                        builder.Append('\t');
                        builder.Append(aluno.Nome);
                        builder.Append('\t');
                        builder.Append(unidade.Unidade.CodigoRedeEnsino);
                        builder.Append('\t');
                        builder.Append(unidade.Unidade.CodigoEscola);
                        builder.Append('\t');
                        builder.Append(unidade.Unidade.NomeEscola);
                        builder.Append('\t');
                        builder.Append(unidade.Distancia.ToString());
                        builder.AppendLine();
                    }
                }
                System.IO.File.WriteAllText(ArquivoRelatorio + "15_escolas.txt", builder.ToString(), Encoding.UTF8);

                Msg("Exportando dados de turmas");
                builder.Clear();
                builder.AppendLine("Diretoria,Município,Rede de Ensino,CIE,Escola,Número Classe,Turma,Série,Período,Tipo de Ensino,Capacidade Máxima,Alocados,Disponível,Excedido");
                foreach (Unidade unidade in unidades.Values)
                {
                    if (unidade.ConsiderarApenasVagasDaUnidade)
                        continue;
                    unidade.RelatorioTurmas(builder);
                }
                if (builder.Length > 0)
                    System.IO.File.WriteAllText(ArquivoRelatorio + "simulacao_turmas.txt", builder.ToString(), Encoding.UTF8);

                Msg("Exportando dados das unidades do município");
                builder.Clear();
                builder.AppendLine("Diretoria,Município,Rede de Ensino,CIE,Escola,Tipo de Ensino,Série,Vagas Ofertadas,Alocados,Vagas Remanescentes,Excedido");
                foreach (Unidade unidade in unidades.Values)
                {
                    if (!unidade.ConsiderarApenasVagasDaUnidade || unidade.CodigoUnidadeCorrigido == PadreTicaoUnidade)
                        continue;
                    unidade.RelatorioTurmas(builder);
                }
                if (builder.Length > 0)
                    System.IO.File.WriteAllText(ArquivoRelatorio + "simulacao_turmas_mun.txt", builder.ToString(), Encoding.UTF8);

                Msg("Exportando dados de alunos");
                builder.Clear();
                StringBuilder builder2 = new StringBuilder();
                int numeroArquivo = 0, contagemAlunos = 1000000;
                Dictionary<int, Dictionary<int, Dictionary<int, int[]>>> totais = new Dictionary<int, Dictionary<int, Dictionary<int, int[]>>>();
                foreach (Aluno aluno in alunos.Values)
                {
                    if (aluno.UnidadeDeOrigem != null)
                    {
                        Dictionary<int, Dictionary<int, int[]>> totais2;
                        if (!totais.TryGetValue(aluno.UnidadeDeOrigem.CodigoRedeEnsino, out totais2))
                        {
                            totais2 = new Dictionary<int, Dictionary<int, int[]>>();
                            totais[aluno.UnidadeDeOrigem.CodigoRedeEnsino] = totais2;
                        }

                        Dictionary<int, int[]> totais3;
                        if (!totais2.TryGetValue(aluno.FaseFicha, out totais3))
                        {
                            totais3 = new Dictionary<int, int[]>();
                            totais2[aluno.FaseFicha] = totais3;
                        }

                        int[] final;
                        if (!totais3.TryGetValue(aluno.TipoEnsinoDesejado | (aluno.SerieDesejada << 16), out final))
                        {
                            final = new int[2];
                            totais3[aluno.TipoEnsinoDesejado | (aluno.SerieDesejada << 16)] = final;
                        }

                        if (aluno.JaAlocado)
                            final[0]++;
                        else
                            final[1]++;
                    }

                    if (contagemAlunos >= 999999)
                    {
                        contagemAlunos = 0;
                        if (builder.Length > 0)
                        {
                            numeroArquivo++;
                            System.IO.File.WriteAllText(ArquivoRelatorio + "simulacao_alunos_" + numeroArquivo + ".txt", builder.ToString(), Encoding.UTF8);
                            builder.Clear();
                        }
                        builder.AppendLine("Aluno\tRA\tCódigo Aluno\tCódigo Ficha\tFase Ficha\tTipo\tDeficiente\tInteresse Noturno\tInteresse Integral\tTipo Ensino Desejado\tSérie Desejada\tTurno Desejado\tDiretoria Origem\tMunicípio Origem\tRede Origem\tCIE Origem\tEscola Origem\tUnidade Origem\tCódigo Turma Origem\tTurno Origem\tDuração Origem\tId Turma Origem\tSala Origem\tSem Unidade Próxima (Def/Inscr)\tAlocado em Unidade Diferente da Inscr\tUnidade Anterior 089\tDiretoria Alocada\tMunicípio Alocado\tRede Alocada\tCIE Alocada\tEscola Alocada\tUnidade Alocada\tAcessível\tClasse Alocada\tTurma Alocada\tId Turma Alocada\tSala Alocada\tMotivo");
                    }
                    aluno.Relatorio(builder);
                    contagemAlunos++;
                }

                builder2.AppendLine($@"""Rede"",""Fase"",""Ensino"",""Série"",""Alocados"",""Pendentes""");
                foreach (KeyValuePair<int, Dictionary<int, Dictionary<int, int[]>>> par in totais)
                {
                    foreach (KeyValuePair<int, Dictionary<int, int[]>> par2 in par.Value)
                    {
                        foreach (KeyValuePair<int, int[]> par3 in par2.Value)
                        {
                            builder2.AppendLine($"{par.Key},{par2.Key},{TipoEnsinoSerie.TipoEnsino(par3.Key)},{TipoEnsinoSerie.Serie(par3.Key)},{par3.Value[0]},{par3.Value[1]}");
                        }
                    }
                }

                System.IO.File.WriteAllText(ArquivoRelatorio + "simulacao_alunos_totais.txt", builder2.ToString(), Encoding.UTF8);
                if (builder.Length > 0)
                {
                    numeroArquivo++;
                    System.IO.File.WriteAllText(ArquivoRelatorio + "simulacao_alunos_" + numeroArquivo + ".txt", builder.ToString(), Encoding.UTF8);
                    builder.Clear();
                }

                Msg("Exportando dados de Vagas");
                using (IDataBase db = FactoryDataBase.Create(ConnectionStringWrite))
                {
                    List<Vagas> vagas = new List<Vagas>();
                    db.ClearParameters();

                    foreach (Unidade unidade in unidades.Values)
                    {
                        if (!unidade.ConsiderarApenasVagasDaUnidade)
                            continue;

                        foreach (KeyValuePair<int, int> par in unidade.VagasTiposEnsinoSeries)
                        {
                            int tipoEnsino = TipoEnsinoSerie.TipoEnsino(par.Key);
                            int serie = TipoEnsinoSerie.Serie(par.Key);
                            vagas.Add(new Vagas()
                            {
                                anoLetivo = AnoLetivoStr,
                                codigoEscola = unidade.CodigoEscola,
                                codigoUnidade = unidade.CodigoUnidadeNoBanco,
                                codigoTipoEnsino = tipoEnsino,
                                serie = serie,
                                totalVagas = par.Value
                            });
                        }
                    }

                    //Grava Historico das vagas
                    VagasMunicipioCompatibilizadas(db, vagas, idRodada);
                }
                #endregion

                if (simular) return;

                #region Exportação de Dados para o Banco
                Msg("Abrindo o banco");
                using (IDataBase db = FactoryDataBase.Create(ConnectionStringWrite))
                {
                    db.SetCommandTimeout(0);
                    int alunosParaMatricular = 0;

                    #region Gravar Log Vagas
                    Msg("Gravar Log Vagas");
                    db.ClearParameters();
                    db.AddParameter("@DT_ANO_LETIVO", AnoLetivo, DbType.AnsiString);
                    db.AddParameter("@ID_RODADA", idRodada);
                    db.ExecuteNonQueryCommandText(Query.QueryLogVagas);
                    #endregion

                    #region Resultado Geral
                    Msg("Contando alunos para matricular");
                    foreach (Aluno aluno in alunos.Values)
                    {
                        if (aluno.TurmaAlocada == null ||
                            aluno.UnidadeAlocada.ConsiderarApenasVagasDaUnidade ||
                            aluno.Congelado)
                            continue;

                        alunosParaMatricular++;
                    }

                    Msg($"Gerando {alunosParaMatricular} ids de matricula");

                    if (alunosParaMatricular > 0)
                    {
                        long idMatricula = (long)db.ExecuteScalarCommandText($@"
					DECLARE @range_first_value sql_variant,
					@range_first_value_output sql_variant;

					EXEC sp_sequence_get_range
						@sequence_name = N'DB_SARA.CADALUNOS.SQ_TB_MATRICULA_ALUNO',
						@range_size = {alunosParaMatricular},
						@range_first_value = @range_first_value_output OUTPUT;

					SELECT @range_first_value_output AS FirstNumber;
					");

                        foreach (Aluno aluno in alunos.Values)
                        {
                            if (aluno.TurmaAlocada == null ||
                                aluno.UnidadeAlocada.ConsiderarApenasVagasDaUnidade ||
                                aluno.Congelado)
                                continue;

                            aluno.CodigoMatriculaSendoCriada = idMatricula++;
                        }
                    }
                    Msg("Gerando dados das alocacoes");
                    using (System.Data.SqlClient.SqlBulkCopy bulk = new System.Data.SqlClient.SqlBulkCopy((System.Data.SqlClient.SqlConnection)db.Connection))
                    {
                        bulk.BulkCopyTimeout = 0;
                        bulk.DestinationTableName = "CALCULO_ROTAS..TB_REL_COMPAT_REAL";

                        DataTable tbl = new DataTable("TB_REL_COMPAT_REAL");
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(long),
                            ColumnName = "ID_REL_COMPAT_REAL",
                            AutoIncrement = true
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "CD_DIRETORIA"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(string),
                            ColumnName = "NM_DIRETORIA"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "CD_DNE"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(string),
                            ColumnName = "NM_MUNICIPIO"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "CD_REDE_ENSINO"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(string),
                            ColumnName = "DS_REDE_ENSINO"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "CD_ESCOLA"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(string),
                            ColumnName = "NM_COMPLETO_ESCOLA"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "CD_UNIDADE"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "CD_TIPO_ENSINO"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(string),
                            ColumnName = "NM_TIPO_ENSINO"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "NR_SERIE"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "CD_TURMA"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "NR_CLASSE"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "CD_ALUNO"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(string),
                            ColumnName = "NM_ALUNO"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(string),
                            ColumnName = "NR_RA"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "ID_FICHA"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "ID_FASE"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(bool),
                            ColumnName = "DEFICIENTE"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "CD_IRMAO"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(bool),
                            ColumnName = "GEMEO"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(bool),
                            ColumnName = "ALOCADO"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(string),
                            ColumnName = "MOTIVO"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(DateTime),
                            AllowDBNull = true,
                            ColumnName = "DT_ALOC"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(decimal),
                            AllowDBNull = true,
                            ColumnName = "CD_ESCOL_ALOC"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(decimal),
                            AllowDBNull = true,
                            ColumnName = "NUM_CLASSE_ALOC"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(decimal),
                            AllowDBNull = true,
                            ColumnName = "CD_ESCOL_COMP_DEF"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(decimal),
                            AllowDBNull = true,
                            ColumnName = "CD_UNID_COMP_DEF"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(string),
                            AllowDBNull = true,
                            ColumnName = "FL_FASE_COMP"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(string),
                            AllowDBNull = true,
                            ColumnName = "TP_COMP_DEF"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(string),
                            AllowDBNull = true,
                            ColumnName = "FL_COMP_DEF"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(DateTime),
                            AllowDBNull = true,
                            ColumnName = "DT_COMP_DEF"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(string),
                            AllowDBNull = true,
                            ColumnName = "HR_COMP_DEF"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(decimal),
                            AllowDBNull = true,
                            ColumnName = "ID_SIT_COMP_DEF"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(string),
                            AllowDBNull = true,
                            ColumnName = "SITUACAO_COMP_DEF"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "ID_RODADA"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(long),
                            ColumnName = "CD_MATRICULA_ALUNO"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(long),
                            AllowDBNull = true,
                            ColumnName = "CD_MATRICULA_ALUNO_BKP_FI",
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(bool),
                            ColumnName = "USOU_LATLNG_DA_ESCOLA"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(float),
                            AllowDBNull = true,
                            ColumnName = "LAT_COMP_DEF"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(float),
                            AllowDBNull = true,
                            ColumnName = "LNG_COMP_DEF"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(float),
                            AllowDBNull = true,
                            ColumnName = "DISTANCIA_COMP_DEF"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(long),
                            AllowDBNull = true,
                            ColumnName = "CD_ENDERECO_ALUNO"
                        });

                        foreach (Aluno aluno in alunos.Values)
                            aluno.ExportarDados(tbl, idRodada);

                        //Msg($"Apagando dados antigos das alocacoes");

                        //db.ExecuteNonQueryCommandText("DELETE FROM CALCULO_ROTAS..TB_REL_COMPAT_REAL WHERE ID_RODADA = " + idRodada);

                        Msg($"Gravando dados novos das alocacoes");

                        bulk.WriteToServer(tbl);
                    }
                    GC.Collect();
                    #endregion

                    #region Atualização das Vagas do Município de SP

                    //Atualiza com os dados da tabela de historico, para ficar mais rapido
                    db.ExecuteNonQueryCommandText($@" UPDATE V SET V.VAGAS = VC.VAGAS
                                                      FROM CADALUNOS.DBO.TB_REL_COMPAT_MUN_VAGAS_COMPATIBILIZADO VC (NOLOCK)
                                                      INNER JOIN CADALUNOS.DBO.TB_REL_COMPAT_MUN_VAGAS V (NOLOCK) ON V.DT_ANO_LETIVO = VC.DT_ANO_LETIVO
												 		                                                        AND V.CD_ESCOLA = VC.CD_ESCOLA
												 		                                                        AND V.CD_UNIDADE =  VC.CD_UNIDADE
												 		                                                        AND V.CD_TIPO_ENSINO = VC.CD_TIPO_ENSINO
												 		                                                        AND V.NR_SERIE = VC.NR_SERIE
                                                      WHERE VC.ID_RODADA = {idRodada}");
                    #endregion

                    #region Atualização do Status dos Alunos
                    Msg("Atualizando status dos alunos e das fichas");
                    db.ClearParameters();
                    db.AddParameter("@DT_ALOC", dataHoraCompat);
                    db.AddParameter("@DT_FIM_MATRICULA", dataCompat.AddDays(-1));
                    db.ExecuteNonQueryCommandText($@"
				-- ANTES DE PROSSEGUIR, FAZ BACKUP DE TODAS AS FICHAS!
				UPDATE RE SET
				    RE.DT_ALOC                      = FI.DT_ALOC,
				    RE.CD_ESCOL_ALOC                = FI.CD_ESCOL_ALOC,
				    RE.NUM_CLASSE_ALOC              = FI.NUM_CLASSE_ALOC,
				    RE.CD_ESCOL_COMP_DEF            = FI.CD_ESCOL_COMP_DEF,
				    RE.CD_UNID_COMP_DEF             = FI.CD_UNID_COMP_DEF,
				    RE.FL_FASE_COMP                 = FI.FL_FASE_COMP,
				    RE.TP_COMP_DEF                  = FI.TP_COMP_DEF,
				    RE.FL_COMP_DEF                  = FI.FL_COMP_DEF,
				    RE.DT_COMP_DEF                  = FI.DT_COMP_DEF,
				    RE.HR_COMP_DEF                  = FI.HR_COMP_DEF,
				    RE.ID_SIT_COMP_DEF              = FI.ID_SIT_COMP_DEF,
				    RE.SITUACAO_COMP_DEF            = FI.SITUACAO_COMP_DEF,
				    RE.CD_MATRICULA_ALUNO_BKP_FI    = FI.CD_MATRICULA_ALUNO
				FROM CALCULO_ROTAS..TB_REL_COMPAT_REAL RE
				INNER JOIN CADALUNOS..TB_FICHA_INSCRICAO FI ON FI.ANO_LETIVO = '{AnoLetivo}'
				AND FI.ID_FICHA_INSCRICAO = RE.ID_FICHA
				WHERE RE.ID_FICHA <> 0
				AND RE.ID_RODADA = {idRodada}

				-- ATUALIZA O STATUS NA TABELA DE ALUNOS
				UPDATE A
				SET A.FL_COMPAT          = 1, 
                    A.FL_COMPAT_ALOC     = RE.ALOCADO
				FROM CALCULO_ROTAS..TB_REL_COMPAT_REAL RE
				INNER JOIN DB_SARA.CADALUNOS.TB_ALUNO A ON A.CD_ALUNO = RE.CD_ALUNO
				WHERE RE.ID_RODADA = {idRodada}

				-- FICHAS DE ALUNOS ALOCADOS E JÁ MATRICULADOS NAS TURMAS
				UPDATE FI
				SET FI.DT_ALOC                  = @DT_ALOC, 
                    FI.CD_ESCOL_ALOC            = RE.CD_ESCOLA, 
                    FI.NUM_CLASSE_ALOC          = RE.NR_CLASSE,
                    FI.CD_UNID_COMP_DEF         = RE.CD_UNIDADE,
				    FI.CD_MATRICULA_ALUNO       = RE.CD_MATRICULA_ALUNO, 
                    FI.SITUACAO_COMP_DEF        = ''
				FROM CALCULO_ROTAS..TB_REL_COMPAT_REAL RE
				INNER JOIN CADALUNOS..TB_FICHA_INSCRICAO FI ON FI.ANO_LETIVO = '{AnoLetivo}'
				AND FI.ID_FICHA_INSCRICAO = RE.ID_FICHA
				WHERE
				RE.ALOCADO = 1
				AND RE.ID_FICHA <> 0
				AND RE.CD_TURMA <> 0
				AND RE.ID_RODADA = {idRodada}

				-- FICHAS DE ALUNOS ALOCADOS EM ALGUMA UNIDADE (INCLUI OS CASOS ACIMA)
				UPDATE FI
				SET FI.CD_ESCOL_COMP_DEF    = RE.CD_ESCOLA, 
                    FI.CD_UNID_COMP_DEF     = RE.CD_UNIDADE,
				    FI.FL_FASE_COMP         = 'D', 
                    FI.TP_COMP_DEF          = 'A', 
                    FI.FL_COMP_DEF          = '1', 
                    FI.DT_COMP_DEF          = @DT_ALOC, 
                    FI.HR_COMP_DEF          = '{dataCompatStrHora}', 
                    FI.ID_SIT_COMP_DEF      = 1,
				    FI.SITUACAO_COMP_DEF    = RE.MOTIVO
				FROM CALCULO_ROTAS..TB_REL_COMPAT_REAL RE
				INNER JOIN CADALUNOS..TB_FICHA_INSCRICAO FI ON FI.ANO_LETIVO = '{AnoLetivo}'
				AND FI.ID_FICHA_INSCRICAO = RE.ID_FICHA
				WHERE
				RE.ALOCADO = 1
				AND RE.ID_FICHA <> 0
				AND RE.ID_RODADA = {idRodada}

				-- FICHAS DE ALUNOS NÃO ALOCADOS
				UPDATE FI
				SET FI.CD_ESCOL_COMP_DEF        = 0, 
                    FI.CD_UNID_COMP_DEF         = 0,
				    FI.FL_FASE_COMP             = 'D', 
                    FI.TP_COMP_DEF              = 'A', 
                    FI.FL_COMP_DEF              = '1', 
                    FI.DT_COMP_DEF              = @DT_ALOC, 
                    FI.HR_COMP_DEF              = '{dataCompatStrHora}', 
                    FI.ID_SIT_COMP_DEF = 2,
				    FI.SITUACAO_COMP_DEF = RE.MOTIVO
				FROM CALCULO_ROTAS..TB_REL_COMPAT_REAL RE
				INNER JOIN CADALUNOS..TB_FICHA_INSCRICAO FI ON FI.ANO_LETIVO = '{AnoLetivo}'
				AND FI.ID_FICHA_INSCRICAO = RE.ID_FICHA
				WHERE
				RE.ALOCADO = 0
				AND RE.ID_FICHA <> 0
				AND RE.ID_RODADA = {idRodada}

                -- ANTES DE PROSSEGUIR COM A CRIAÇÃO DAS NOVAS MATRÍCULAS, DEVEMOS MARCAR COMO TRANSFERIDO TODAS
				-- AS MATRÍCULAS EXISTENTES DO PESSOAL QUE FOI ALOCADO NO DESLOCAMENTO (FASE 0, 9)
				DELETE FROM CALCULO_ROTAS..TB_REL_COMPAT_TRANSFERENCIA_MOVIMENTACAO WHERE ID_RODADA = {idRodada}
				INSERT INTO CALCULO_ROTAS..TB_REL_COMPAT_TRANSFERENCIA_MOVIMENTACAO (CD_MATRICULA_ALUNO, CD_ALUNO, ID_RODADA)
				SELECT MA.CD_MATRICULA_ALUNO, MA.CD_ALUNO, {idRodada}
				FROM CALCULO_ROTAS..TB_REL_COMPAT_REAL RE WITH(NOLOCK)
				INNER JOIN DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA WITH(NOLOCK) ON MA.DT_ANO_LETIVO = '{AnoLetivo}'
				AND MA.CD_ALUNO = RE.CD_ALUNO
				AND MA.FL_SITUACAO_ALUNO_CLASSE = 0
				-- As matrículas abaixo devem ser preservadas
				AND MA.NR_GRAU NOT IN (58, 35, 7, 8, 9, 10, 16, 33, 25, 30, 40, 50, 39, 42, 43, 44)
				WHERE
				RE.ALOCADO = 1
				AND RE.ID_FICHA <> 0
				AND RE.CD_TURMA <> 0 -- Derruba apenas as matrículas dos alunos efetivamente matriculados (só alocados na escola, não derruba)
				AND RE.ID_FASE IN (0, 9)
				AND RE.ID_RODADA = {idRodada}

				-- EXCLUI AS MATRÍCULAS EXISTENTES DO PESSOAL DE DESLOCAMENTO
				UPDATE MA
				SET MA.FL_SITUACAO_ALUNO_CLASSE     = 1,
				MA.DT_FIM_MATRICULA                 = @DT_FIM_MATRICULA,
				MA.DT_ALTER_1                       = @DT_ALOC,
				MA.LOGIN_ALTER                      = '[AUTO COMPAT DESLOC]',
				MA.MACHINE_ALTER                    = '[AUTO COMPAT DESLOC]',
				MA.USER_ALTER                       = '[AUTO COMPAT DESLOC]'
				FROM CALCULO_ROTAS..TB_REL_COMPAT_TRANSFERENCIA_MOVIMENTACAO TMP
				INNER JOIN DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA ON MA.DT_ANO_LETIVO = '{AnoLetivo}'
				AND MA.CD_MATRICULA_ALUNO = TMP.CD_MATRICULA_ALUNO
				WHERE TMP.ID_RODADA = {idRodada}
				
				-- ANTES DE PROSSEGUIR COM A CRIAÇÃO DAS NOVAS MATRÍCULAS, DEVEMOS EXCLUIR TODAS
				-- AS MATRÍCULAS EXISTENTES DO PESSOAL QUE FOI ALOCADO NO DESLOCAMENTO (8)
				DELETE FROM CADALUNOS..TB_REL_COMPAT_EXCLUSAO_DESLOCAMENTO WHERE ID_RODADA = {idRodada}

				INSERT INTO CADALUNOS..TB_REL_COMPAT_EXCLUSAO_DESLOCAMENTO (CD_MATRICULA_ALUNO, CD_ALUNO, ID_RODADA)
				SELECT MA.CD_MATRICULA_ALUNO, MA.CD_ALUNO, {idRodada}
				FROM CALCULO_ROTAS..TB_REL_COMPAT_REAL RE WITH(NOLOCK)
				INNER JOIN DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA WITH(NOLOCK) ON MA.DT_ANO_LETIVO = '{AnoLetivo}'
				AND MA.CD_ALUNO = RE.CD_ALUNO
				AND MA.FL_SITUACAO_ALUNO_CLASSE = 0
				-- As matrículas abaixo devem ser preservadas
				AND MA.NR_GRAU NOT IN (58, 35, 7, 8, 9, 10, 16, 33, 25, 30, 40, 50, 39, 42, 43, 44)
				WHERE
				RE.ALOCADO = 1
				AND RE.ID_FICHA <> 0
				AND RE.CD_TURMA <> 0 -- Derruba apenas as matrículas dos alunos efetivamente matriculados (só alocados na escola, não derruba)
				AND RE.ID_FASE = 8
				AND RE.ID_RODADA = {idRodada}

				-- EXCLUI AS MATRÍCULAS EXISTENTES DO PESSOAL DE DESLOCAMENTO
				UPDATE MA
				SET MA.FL_SITUACAO_ALUNO_CLASSE     = 99,
				MA.DT_FIM_MATRICULA                 = @DT_FIM_MATRICULA,
                MA.DT_EXCL                          = @DT_ALOC,
                MA.LOGIN_EXCL                       = '[AUTO COMPAT DESLOC]',
				MA.MACHINE_EXCL                     = '[AUTO COMPAT DESLOC]',
				MA.USER_EXCL                        = '[AUTO COMPAT DESLOC]'
				FROM CADALUNOS..TB_REL_COMPAT_EXCLUSAO_DESLOCAMENTO TMP
				INNER JOIN DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA ON MA.DT_ANO_LETIVO = '{AnoLetivo}'
				AND MA.CD_MATRICULA_ALUNO = TMP.CD_MATRICULA_ALUNO
				WHERE TMP.ID_RODADA = {idRodada}

                -- EXCLUI AS MATRÍCULAS ANTECIPADAS EXISTENTES DO PESSOAL DE DESLOCAMENTO
				UPDATE MA
				SET MA.FL_SITUACAO_ALUNO_CLASSE     = 99,
				    MA.DT_FIM_MATRICULA             = @DT_FIM_MATRICULA,
                    MA.DT_EXCL                      = @DT_ALOC,
                    MA.LOGIN_EXCL                   = '[AUTO COMPAT DESLOC]',
				    MA.MACHINE_EXCL                 = '[AUTO COMPAT DESLOC]',
				    MA.USER_EXCL                    = '[AUTO COMPAT DESLOC]'
				FROM CADALUNOS..TB_REL_COMPAT_EXCLUSAO_DESLOCAMENTO TMP
				INNER JOIN DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO MA 
                    ON MA.DT_ANO_LETIVO = '{AnoLetivo + 1}'
				    AND MA.CD_ALUNO = TMP.CD_ALUNO
				WHERE TMP.ID_RODADA = {idRodada}
                    AND MA.FL_SITUACAO_ALUNO_CLASSE = 0

				");
                    db.ClearParameters();
                    #endregion

                    #region Inclusão das Matrículas
                    Msg("Gerando dados das matriculas");
                    using (System.Data.SqlClient.SqlBulkCopy bulk = new System.Data.SqlClient.SqlBulkCopy((System.Data.SqlClient.SqlConnection)db.Connection))
                    {
                        bulk.BulkCopyTimeout = 0;
                        bulk.DestinationTableName = "DB_SARA.CADALUNOS.TB_MATRICULA_ALUNO";

                        DataTable tbl = new DataTable("TB_MATRICULA_ALUNO");
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(long),
                            ColumnName = "CD_MATRICULA_ALUNO"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "CD_ALUNO"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "NR_SERIE"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "FL_SITUACAO_ALUNO_CLASSE"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "CD_TURMA"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "CD_ESCOLA"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "NR_GRAU"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "NR_ALUNO"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(DateTime),
                            ColumnName = "DT_INICIO_MATRICULA"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(DateTime),
                            ColumnName = "DT_FIM_MATRICULA"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "CD_GRAU_NIVEL"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "CD_SERIE_NIVEL"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(string),
                            ColumnName = "DT_ENVIO"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "NR_CLASSE"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(DateTime),
                            ColumnName = "DT_INCL"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(string),
                            ColumnName = "HR_INCL"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(string),
                            ColumnName = "HR_ENVIO"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(DateTime),
                            ColumnName = "DT_INCL_MATRIC"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(string),
                            ColumnName = "HR_INCL_MATRIC"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(string),
                            ColumnName = "DT_ANO_LETIVO"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(string),
                            ColumnName = "LOGIN_INCL"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(string),
                            ColumnName = "MACHINE_INCL"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(string),
                            ColumnName = "USER_INCL"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(bool),
                            ColumnName = "FL_COMPAT_MANUAL"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "CD_TIPO_ENSINO_EQUIVALENTE"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            ColumnName = "NR_SERIE_EQUIVALENTE"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(long),
                            AllowDBNull = true,
                            ColumnName = "CD_MATRICULA_ALUNO_ANTERIOR"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            AllowDBNull = true,
                            ColumnName = "ID_FICHA_INSCRICAO"
                        });
                        tbl.Columns.Add(new DataColumn()
                        {
                            DataType = typeof(int),
                            AllowDBNull = true,
                            ColumnName = "CD_TIPO_EXCECAO"
                        });

                        foreach (Aluno aluno in alunos.Values)
                        {
                            if (aluno.TurmaAlocada == null ||
                                aluno.UnidadeAlocada.ConsiderarApenasVagasDaUnidade ||
                                aluno.Congelado)
                                continue;

                            aluno.Matricular(tbl, AnoLetivoStr, dataHoraCompat, dataCompat, dataCompatStrYMA, dataCompatStrHora5, Rodadas);
                        }

                        Msg($"Gravando dados das matriculas");

                        // Mapeia as colunas para não depender apenas da ordem das colunas em tbl.Columns
                        foreach (DataColumn c in tbl.Columns)
                            bulk.ColumnMappings.Add(c.ColumnName, c.ColumnName);

                        bulk.WriteToServer(tbl);
                    }
                    GC.Collect();
                    #endregion

                    #region Atualização Quantidades Alunos Turma
                    Program.Msg($"Atualizando quantidades nas turmas");
                    db.ExecuteNonQueryCommandText(Query.QueryAtualizaTurmaQtds);
                    #endregion

                    #region Inativando Matriculas ativas em outras redes
                    Program.Msg($"Inativando Matriculas ativas em outras redes");
                    db.ClearParameters();
                    db.AddParameter("@ID_RODADA", idRodada);
                    db.AddParameter("@DT_ANO_LETIVO", AnoLetivo, DbType.AnsiString);
                    db.AddParameter("@DT_FIM_MATRICULA", dataCompat.AddDays(-1));
                    db.ExecuteNonQueryCommandText(Query.QueryInativarMatriculaOutrasRedes);
                    #endregion

                    #region Conversao do Abandono
                    if (ConversaoAbandono)
                    {
                        Program.Msg($"Conversao do Abandono");
                        db.ClearParameters();
                        db.AddParameter("@ID_RODADA", idRodada);
                        db.AddParameter("@DT_ANO_LETIVO", AnoLetivo.ToString(), DbType.AnsiString);
                        db.ExecuteNonQueryCommandText(Query.QueryConversaoAbandono);
                    }
                    #endregion

                    #region Inativando Matriculas e Inscrições dos alunos do ano seguinte matriculados pela compatibilização
                    if (DateTime.Now.Month >= 10)
                    {
                        Program.Msg($"Inativando Matriculas e Inscrições do ano seguinte dos alunos matriculados pela compatibilização");
                        db.ClearParameters();
                        db.AddParameter("@ID_RODADA", idRodada);
                        db.AddParameter("@DT_ANO_LETIVO_ANO_SEGUINTE", AnoLetivo + 1, DbType.AnsiString);
                        db.ExecuteNonQueryCommandText(Query.QueryMatInscAnoSeguinte);
                    }
                    #endregion

                    #region Finaliza a compatibilização
                    Msg("Fim Compatibilização");
                    db.ExecuteNonQueryCommandText(Query.QueryFinalizaCompatibiarra);
                    #endregion

                    #region INTEGRACAO SED MUN
                    if (gerarCBI)
                    {
                        Msg("Incluindo registros na tabela TB_INTEGRACAO_SED_MUN");
                        db.ClearParameters();
                        db.AddParameter("@ID_RODADA", idRodada);
                        db.ExecuteNonQueryCommandText(Query.QueryIntegracaoSedMun);
                    }
                    #endregion

                    #region Inativando Interesse de Rematricula
                    if (InativarInteresseRematricula)
                    {
                        Program.Msg($"Inativando Interesse de Rematricula");
                        db.ClearParameters();
                        db.AddParameter("@ID_RODADA", idRodada);
                        db.AddParameter("@DT_ANO_LETIVO", AnoLetivo, DbType.AnsiString);
                        db.AddParameter("@DT_ANO_LETIVO_INTERESSE", AnoLetivo + 1, DbType.AnsiString);
                        db.ExecuteNonQueryCommandText(Query.QueryInativarInteresseRematricula);
                    }
                    #endregion
                    if (rodadas)
                    {
                        #region Envio de Email
                        if (EnviarEmail)
                        {
                            Msg("Envio de Email.");
                            DisparoEmail(db, idRodada);
                        }
                        #endregion
                    }

                }
                #endregion
            }
        }

        /// <summary>
        /// Busca no banco de dados a ordem de execução dos alunos
        /// </summary>
        /// <param name="db"></param>
        /// <param name="ordemExecucao"></param>
        private static void CarregarAlunoEscolaDistancia(IDataBase db, List<Ordem> ordemExecucao)
        {
            Msg("Carregando Lista de Ordem de execução - Distancia.");
            using (SedDataReader reader = db.ExecuteSedReaderCommandText(Query.QueryAlunoEscolaDistancia))
            {
                while (reader.Read())
                {
                    ordemExecucao.Add(new Ordem()
                    {
                        id = reader.GetInt64NonDBNull(0),
                        codigoAluno = reader.GetInt32NonDBNull(1),
                        codigoEscola = reader.GetInt32NonDBNull(2),
                        codigoUnidade = reader.GetInt32NonDBNull(3),
                        distancia = reader.GetDoubleNonDBNull(4),
                        continuidade = reader.GetBooleanNonDBNull(5),
                        codigoEnderecoAluno = reader.GetInt64NonDBNull(6),
                    });
                }
            }
        }

        /// <summary>
        /// Busca no banco de dados a ordem de execução dos alunos sem Rodadas
        /// </summary>
        /// <param name="db"></param>
        /// <param name="ordemExecucao"></param>
        private static void CarregarAlunoEscolaDistanciaSemRodada(IDataBase db, List<Ordem> ordemExecucao)
        {
            Msg("Carregando Lista de Ordem de execução - Sem rodada.");
            using (SedDataReader reader = db.ExecuteSedReaderCommandText(Query.QueryAlunoEscolaDistanciaSemRodadaAPENASAPE))
            {
                while (reader.Read())
                {
                    ordemExecucao.Add(new Ordem()
                    {
                        id = reader.GetInt64NonDBNull(0),
                        codigoAluno = reader.GetInt32NonDBNull(1),
                        codigoEscola = reader.GetInt32NonDBNull(2),
                        codigoUnidade = reader.GetInt32NonDBNull(3),
                        distancia = reader.GetDoubleNonDBNull(4),
                        continuidade = reader.GetBooleanNonDBNull(5),
                        codigoEnderecoAluno = reader.GetInt64NonDBNull(6),
                    });
                }
            }
        }

        /// <summary>
        /// Metodo para validar o layout dos arquivos CEU e PADRE TICAO
        /// </summary>
        private static void testeArquivosPadreTicaoECeu()
        {
            //CEU
            int alunosTotais = 0;
            if (System.IO.File.Exists(ArquivoCEU))
            {
                foreach (string linha in System.IO.File.ReadAllLines(ArquivoCEU))
                {
                    if (string.IsNullOrWhiteSpace(linha))
                        continue;

                    string[] partes = linha.Split('\t');
                    if (partes.Length != 3)
                        continue;

                    string raNormalizado = Aluno.NormalizarRA(partes[0]);

                    //RA Escola Unidade
                    int escola, unidadeOriginal;
                    if (!int.TryParse(partes[1], out escola) ||
                        !int.TryParse(partes[2], out unidadeOriginal))
                        continue;

                    alunosTotais++;
                }

                Msg($"Total de Ra's Encontrado no arquivo in.ceu.txt {alunosTotais}");
            }
            else
            {
                Msg("Arquivo in.ceu.txt nao foi(ram) encontrado(s)");
            }

            //Padre Ticao
            if (System.IO.File.Exists(ArquivoPadreTicao))
            {
                alunosTotais = 0;
                foreach (string ra in System.IO.File.ReadAllLines(ArquivoPadreTicao))
                {
                    if (string.IsNullOrWhiteSpace(ra)) continue;
                    alunosTotais++;


                    string raNormalizado = Aluno.NormalizarRA(ra);
                }

                Msg($"Total de Ra's Encontrado no arquivo in.padreticao.txt {alunosTotais}");
            }
            else
            {
                Msg("Arquivo in.padreticao.txt nao foi encontrado");
            }
        }
        private static bool verifica_execucao(string nome_processo)
        {
            Process[] processes;
            bool rodando = false;
            try
            {
                processes = Process.GetProcessesByName(nome_processo);
                if (processes != null)
                {
                    if (processes.GetLength(0) >= 1)
                    {
                        rodando = true;
                    }
                }
            }
            catch { }
            return rodando;
        }

        static void Main(string[] args)
        {
            try
            {

                //Não deixa iniciar enquanto o calculo de rotas tiver em execução
                while (verifica_execucao("See.Sed.FichaAluno.CalculoRotas") || verifica_execucao("see.sed.fichaaluno.calculorotas"))
                {
                    Msg("Calculo de rotas em execuçao, aguardando...");
                    System.Threading.Thread.Sleep(60 * 1000);
                }

                Dictionary<int, Unidade> unidades = new Dictionary<int, Unidade>(40000);
                alunos = new Dictionary<int, Aluno>(4000000);
                List<Ordem> OrdemExecucao = new List<Ordem>();
                sLog = new StringBuilder();
                StringBuilder strMsg = new StringBuilder();
                // UTILIZADO POR CAUSA DO TASK SCHEDULER
                if (args.Length > 0)
                {
                    string command = Convert.ToString(args[0]).ToUpper();
                    switch (command)
                    {
                        case "AUTOMATICA":
                            CompatibilizacaoAutomatica();
                            return;
                        case "2":
                            Msg("Opção Selecionada: 2 - Alocar alunos sem rodadas");
                            Msg($"Fases Selecionadas - {Program.FasesCompatibilizacaoCalculo2}");
                            Msg($"CD_MOTIVO FASE 8 - {Program.MotivoFase8}");

                            using (IDataBase db = FactoryDataBase.Create(ConnectionStringRead))
                            {
                                db.SetCommandTimeout(0);
                                CarregarUnidadesEAlunos(db, unidades, false, false);
                                CarregarAlunoEscolaDistanciaSemRodada(db, OrdemExecucao);
                            }
                            if (OrdemExecucao.Count <= 0)
                            {
                                Msg("Nao foi possivel carregar a tabela de Execução!");
                            }
                            else
                            {
                                AlocarAlunos(OrdemExecucao, unidades, simular, false, false);
                                AlocarAlunosIgnoraEscolha(OrdemExecucao, unidades, simular, false);
                            }
                            Msg("Fim");
                            GravaLog();
                            strMsg = new StringBuilder();
                            strMsg.Append("Olá, <br />" + Environment.NewLine);
                            strMsg.Append("Esta é uma notificação de que a compatibilização terminou a execução, segue abaixo o log: <br /><br />" + Environment.NewLine);
                            strMsg.Append(sLog.ToString());

                            Util.Mail.EnviarEmailBodyHtmlGmail(ConfigurationManager.AppSettings["EmailLiderTecnico"].ToString(), ConfigurationManager.AppSettings["DestinatariosAvisoExecucao"].ToString(), "Compatibilização Automática (Tipo 2) - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), strMsg.ToString().Replace(System.Environment.NewLine, "<br />"));
                            return;
                        case "3":
                            Msg("Opção Selecionada: 3 - Alocar alunos (rodadas)");
                            using (IDataBase db = FactoryDataBase.Create(ConnectionStringWrite))
                            {
                                db.SetCommandTimeout(0);
                                CarregarUnidadesEAlunos(db, unidades, false, true);
                                CarregarAlunoEscolaDistancia(db, OrdemExecucao);
                            }
                            if (OrdemExecucao.Count <= 0)
                            {
                                Msg("Nao foi possivel carregar a tabela de Execução!");
                            }
                            else
                            {
                                AlocarAlunos(OrdemExecucao, unidades, simular, false, true);
                                AlocarAlunosIgnoraEscolha(OrdemExecucao, unidades, simular, true);
                            }
                            Msg("Executando processo de transferência RPP");
                            try
                            {
                                using (IDataBase db = FactoryDataBase.Create(ConnectionStringWrite))
                                {
                                    db.SetCommandTimeout(0);
                                    TransferenciaRPP(db);
                                }
                                Msg("Fim processo de transferência RPP");
                            }
                            catch (Exception ex)
                            {
                                Msg("Erro: " + ex.Message);
                            }
                            Msg("Executando processo de transferência das notas");
                            try
                            {
                                using (IDataBase db = FactoryDataBase.Create(ConnectionStringWrite))
                                {
                                    db.SetCommandTimeout(0);
                                    TransferenciaNotas(db);
                                }
                                Msg("Fim processo de transferência das notas");
                            }
                            catch (Exception ex)
                            {
                                Msg("Erro: " + ex.Message);
                            }
                            Msg("Executando processo de log de fichas fora do parâmetro");
                            try
                            {
                                using (IDataBase db = FactoryDataBase.Create(ConnectionStringWrite))
                                {
                                    db.SetCommandTimeout(0);
                                    LogFichasForaDoParametro(db);
                                }
                                Msg("Fim processo de log de fichas fora do parâmetro");
                            }
                            catch (Exception ex)
                            {
                                Msg("Erro: " + ex.Message);
                            }

                            Msg("Fim");
                            GravaLog();
                            strMsg = new StringBuilder();
                            strMsg.Append("Olá, <br />" + Environment.NewLine);
                            strMsg.Append("Esta é uma notificação de que a compatibilização terminou a execução, segue abaixo o log: <br /><br />" + Environment.NewLine);
                            strMsg.Append(sLog.ToString());

                            Util.Mail.EnviarEmailBodyHtmlGmail(ConfigurationManager.AppSettings["EmailLiderTecnico"].ToString(), ConfigurationManager.AppSettings["DestinatariosAvisoExecucao"].ToString(), "Compatibilização Automática (Tipo 3) - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), strMsg.ToString().Replace(System.Environment.NewLine, "<br />"));
                            return;
                        case "4":
                            Msg("Opção Selecionada: 4 - Teste e-mail");
                            strMsg = new StringBuilder();
                            strMsg.Append("Olá, <br />" + Environment.NewLine);
                            strMsg.Append("Esta é uma notificação de que a compatibilização enviou email, segue abaixo o log: <br /><br />" + Environment.NewLine);
                            strMsg.Append(sLog.ToString());

                            Util.Mail.EnviarEmailBodyHtmlGmail(ConfigurationManager.AppSettings["EmailLiderTecnico"].ToString(), "", "Compatibilização Tipo 4 - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), strMsg.ToString().Replace(System.Environment.NewLine, "<br />"));
                            return;
                    }

                }



                TipoEnsinoSerie.CarregarEquivalentes();
                GC.Collect();

                /*
                GeraMatriculaAlunosDeslocamentoSemMatricula();
                return;*/

                for (; ; )
                {
                    // Para manter a sanidade, caso o usuário realmente digite várias opções em sequência
                    unidades?.Clear();
                    alunos?.Clear();
                    unidades = new Dictionary<int, Unidade>(40000);
                    alunos = new Dictionary<int, Aluno>(4000000);
                    OrdemExecucao = new List<Ordem>();
                    sLog = new StringBuilder();
                    GC.Collect();


                    int operacao;
                    Console.WriteLine("Operacoes: ");

                    //Console.WriteLine("1 - Gerar/atualizar os arquivos de unidades, turmas, alunos e irmaos");
                    Console.WriteLine("2 - Alocar alunos sem rodadas");
                    Console.WriteLine("3 - Alocar alunos (rodadas)");
                    Console.WriteLine("4 - Teste Email");
                    Console.WriteLine("99 - Sair");
                    do
                    {
                        Console.Write("Digite a operacao: ");
                        int.TryParse(Console.ReadLine().Trim(), out operacao);
                        if (operacao == 99)
                            return;
                    } while (operacao < 2 || operacao > 4);

                    switch (operacao)
                    {
                        case 1:
                            Msg("Opção Selecionada: 1 - Gerar/atualizar os arquivos de unidades, turmas, alunos e irmaos");
                            using (IDataBase db = FactoryDataBase.Create(ConnectionStringRead))
                            {
                                db.SetCommandTimeout(0);
                                CarregarUnidadesEAlunos(db, unidades);
                            }
                            break;
                        case 2:
                            Msg("Opção Selecionada: 2 - Alocar alunos sem rodadas");
                            Msg($"Fases Selecionadas - {Program.FasesCompatibilizacaoCalculo2}");
                            Msg($"CD_MOTIVO FASE 8 - {Program.MotivoFase8}");

                            // if (!CarregarUnidadesEAlunos(null, unidades, ref alunos))
                            // {
                            //Msg("Nao foi possivel carregar todos os arquivos do disco!");
                            //}
                            //else
                            //{
                            using (IDataBase db = FactoryDataBase.Create(ConnectionStringRead))
                            {
                                db.SetCommandTimeout(0);
                                CarregarUnidadesEAlunos(db, unidades, false, false);
                                CarregarAlunoEscolaDistanciaSemRodada(db, OrdemExecucao);
                            }
                            if (OrdemExecucao.Count <= 0)
                            {
                                Msg("Nao foi possivel carregar a tabela de Execução!");
                            }
                            else
                            {
                                AlocarAlunos(OrdemExecucao, unidades, simular, false, false);
                                AlocarAlunosIgnoraEscolha(OrdemExecucao, unidades, simular, false);
                            }
                            strMsg = new StringBuilder();
                            strMsg.Append("Olá, <br />" + Environment.NewLine);
                            strMsg.Append("Esta é uma notificação de que a compatibilização terminou a execução, segue abaixo o log: <br /><br />" + Environment.NewLine);
                            strMsg.Append(sLog.ToString());

                            Util.Mail.EnviarEmailBodyHtmlGmail(ConfigurationManager.AppSettings["EmailLiderTecnico"].ToString(), ConfigurationManager.AppSettings["DestinatariosAvisoExecucao"].ToString(), "Compatibilização manual (Tipo 2) - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), strMsg.ToString().Replace(System.Environment.NewLine, "<br />"));

                            // }
                            break;
                        case 3:
                            Msg("Opção Selecionada: 3 - Alocar alunos (rodadas)");
                            using (IDataBase db = FactoryDataBase.Create(ConnectionStringWrite))
                            {
                                db.SetCommandTimeout(0);
                                CarregarUnidadesEAlunos(db, unidades, false, true);
                                CarregarAlunoEscolaDistancia(db, OrdemExecucao);
                            }
                            if (OrdemExecucao.Count <= 0)
                            {
                                Msg("Nao foi possivel carregar a tabela de Execução!");
                            }
                            else
                            {
                                AlocarAlunos(OrdemExecucao, unidades, simular, false, true);
                                AlocarAlunosIgnoraEscolha(OrdemExecucao, unidades, simular, true);
                            }

                            strMsg = new StringBuilder();
                            strMsg.Append("Olá, <br />" + Environment.NewLine);
                            strMsg.Append("Esta é uma notificação de que a compatibilização terminou a execução, segue abaixo o log: <br /><br />" + Environment.NewLine);
                            strMsg.Append(sLog.ToString());

                            Util.Mail.EnviarEmailBodyHtmlGmail(ConfigurationManager.AppSettings["EmailLiderTecnico"].ToString(), ConfigurationManager.AppSettings["DestinatariosAvisoExecucao"].ToString(), "Compatibilização manual (Tipo 3) - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), strMsg.ToString().Replace(System.Environment.NewLine, "<br />"));

                            Msg("Executando processo de log de fichas fora do parâmetro");
                            try
                            {
                                using (IDataBase db = FactoryDataBase.Create(ConnectionStringWrite))
                                {
                                    db.SetCommandTimeout(0);
                                    LogFichasForaDoParametro(db);
                                }
                                Msg("Fim processo de log de fichas fora do parâmetro");
                            }
                            catch (Exception ex)
                            {
                                Msg("Erro: " + ex.Message);
                            }

                            break;
                        case 4:
                            Msg("Opção Selecionada: 4 - Teste e-mail");
                            strMsg = new StringBuilder();
                            strMsg.Append("Olá, <br />" + Environment.NewLine);
                            strMsg.Append("Esta é uma notificação de que a compatibilização enviou email, segue abaixo o log: <br /><br />" + Environment.NewLine);
                            strMsg.Append(sLog.ToString());

                            Util.Mail.EnviarEmailBodyHtmlGmail(ConfigurationManager.AppSettings["EmailLiderTecnico"].ToString(), "", "Compatibilização Tipo 4 - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), strMsg.ToString().Replace(System.Environment.NewLine, "<br />"));
                            return;
                    }
                    Msg("Executando processo de tranferência RPP" + Environment.NewLine);
                    try
                    {
                        using (IDataBase db = FactoryDataBase.Create(ConnectionStringWrite))
                        {
                            db.SetCommandTimeout(0);
                            TransferenciaRPP(db);
                        }
                        Msg("Fim processo de tranferência RPP" + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        Msg("Erro: " + ex.Message);
                    }
                    Msg("Executando processo de transferência das notas");
                    try
                    {
                        using (IDataBase db = FactoryDataBase.Create(ConnectionStringWrite))
                        {
                            db.SetCommandTimeout(0);
                            TransferenciaNotas(db);
                        }
                        Msg("Fim processo de transferência das notas");
                    }
                    catch (Exception ex)
                    {
                        Msg("Erro: " + ex.Message);
                    }
                    Msg("Fim");
                    GravaLog();
                    Console.WriteLine();
                }
            }
            catch (Exception err)
            {
                Console.WriteLine(err.Message + " " + err.StackTrace);
                GravaLog();
            }
        }

        /// <summary>
        /// Esse Metodo será utilizado quando for compatiblização automatica.
        /// Metodo deve ser identico ao item 3 - Alocar alunos (rodadas)
        /// </summary>
        private static void CompatibilizacaoAutomatica()
        {
            Dictionary<int, Unidade> unidades = new Dictionary<int, Unidade>(30000);
            Dictionary<int, Aluno> alunos = new Dictionary<int, Aluno>(4000000);
            List<Ordem> OrdemExecucao = new List<Ordem>();
            sLog = new StringBuilder();
            TipoEnsinoSerie.CarregarEquivalentes();
            GC.Collect();
            try
            {
                Msg("Compatibilização Automatica");
                using (IDataBase db = FactoryDataBase.Create(ConnectionStringWrite))
                {
                    db.SetCommandTimeout(0);
                    CarregarUnidadesEAlunos(db, unidades, false, true);
                    CarregarAlunoEscolaDistancia(db, OrdemExecucao);
                }
                if (OrdemExecucao.Count <= 0)
                {
                    Msg("Nao foi possivel carregar a tabela de Execução!");
                }
                else
                {
                    AlocarAlunos(OrdemExecucao, unidades, simular, false, true);
                    AlocarAlunosIgnoraEscolha(OrdemExecucao, unidades, simular, true);
                }
                Msg("Fim");
                GravaLog();
                StringBuilder strMsg = new StringBuilder();
                strMsg.Append("Olá, <br />" + Environment.NewLine);
                strMsg.Append("Esta é uma notificação de que a compatibilização terminou a execução, segue abaixo o log: <br /><br />" + Environment.NewLine);
                strMsg.Append(sLog.ToString());

                Util.Mail.EnviarEmailBodyHtmlGmail(ConfigurationManager.AppSettings["EmailLiderTecnico"].ToString(), ConfigurationManager.AppSettings["DestinatariosAvisoExecucao"].ToString(), "Compatibilização Automática - " + DateTime.Now.ToString("dd/MM/yyyy HH:mm"), strMsg.ToString().Replace(System.Environment.NewLine, "<br />"));
            }
            catch (Exception ex)
            {
                sLog.AppendLine(ex.InnerException.Message);
                GravaLog();
                throw;
            }
        }

        /// <summary>
        /// Grava Lod da Compatibilização em TXT
        /// </summary>
        private static void GravaLog()
        {
            string dir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string diretorio = Path.Combine(dir, "LOG");
            if (!Directory.Exists(diretorio)) Directory.CreateDirectory(diretorio);

            string nomeArquivo = @"logCompatibilizacao_" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".txt";
            StreamWriter writerProcesso = new StreamWriter(Path.Combine(diretorio, nomeArquivo));
            writerProcesso.WriteLine(sLog.ToString());
            writerProcesso.Close();
        }

        /// <summary>
        /// Log do consumo de vagas do Municipio
        /// </summary>
        /// <param name="db"></param>
        /// <param name="listaVagas"></param>
        /// <param name="idRodada"></param>
        private static void VagasMunicipioCompatibilizadas(IDataBase db, List<Vagas> listaVagas, int idRodada)
        {

            using (System.Data.SqlClient.SqlBulkCopy bulk = new System.Data.SqlClient.SqlBulkCopy((System.Data.SqlClient.SqlConnection)db.Connection))
            {
                bulk.BulkCopyTimeout = 0;
                bulk.DestinationTableName = "CADALUNOS.DBO.TB_REL_COMPAT_MUN_VAGAS_COMPATIBILIZADO";

                DataTable tbl = new DataTable("TB_REL_COMPAT_MUN_VAGAS_COMPATIBILIZADO");
                tbl.Columns.Add(new DataColumn()
                {
                    DataType = typeof(int),
                    ColumnName = "ID_RODADA"
                });
                tbl.Columns.Add(new DataColumn()
                {
                    DataType = typeof(string),
                    ColumnName = "DT_ANO_LETIVO"
                });
                tbl.Columns.Add(new DataColumn()
                {
                    DataType = typeof(int),
                    ColumnName = "CD_ESCOLA"
                });
                tbl.Columns.Add(new DataColumn()
                {
                    DataType = typeof(int),
                    ColumnName = "CD_UNIDADE"
                });
                tbl.Columns.Add(new DataColumn()
                {
                    DataType = typeof(int),
                    ColumnName = "CD_TIPO_ENSINO"
                });
                tbl.Columns.Add(new DataColumn()
                {
                    DataType = typeof(int),
                    ColumnName = "NR_SERIE"
                });
                tbl.Columns.Add(new DataColumn()
                {
                    DataType = typeof(int),
                    ColumnName = "VAGAS"
                });

                DataRow row;
                foreach (var vaga in listaVagas)
                {
                    row = tbl.NewRow();
                    row["ID_RODADA"] = idRodada;
                    row["DT_ANO_LETIVO"] = vaga.anoLetivo;
                    row["CD_ESCOLA"] = vaga.codigoEscola;
                    row["CD_UNIDADE"] = vaga.codigoUnidade;
                    row["CD_TIPO_ENSINO"] = vaga.codigoTipoEnsino;
                    row["NR_SERIE"] = vaga.serie;
                    row["VAGAS"] = vaga.totalVagas;
                    tbl.Rows.Add(row);
                }

                //Aqui apagamos apenas os dados das escolas que participaram dessa rodada, em caso de rodar a compatibilização 2x para escolas diferentes, todo o histórico é mantido
                string listaEscolas = String.Join(",", listaVagas.Select(x => x.codigoEscola).Distinct());
                db.ExecuteNonQueryCommandText($"DELETE FROM CADALUNOS.DBO.TB_REL_COMPAT_MUN_VAGAS_COMPATIBILIZADO WHERE ID_RODADA = {idRodada} AND CD_ESCOLA IN ({listaEscolas})");

                bulk.WriteToServer(tbl);
            }
            GC.Collect();
        }

        #region EMAIL
        /// <summary>
        /// Disparo de Email
        /// </summary>
        /// <param name="db"></param>
        /// <param name="idRodada"></param>
        private static void DisparoEmail(IDataBase db, int idRodada)
        {
            if (!File.Exists(string.Concat(dir, "\\Template\\Email.html")))
            {
                Msg("Não foi possivel localizar o Template.");
                return;
            }

            string templateEmail = File.ReadAllText(string.Concat(dir, "\\Template\\Email.html"));
            List<Email> emails = new List<Email>();

            emails = BuscarEmails(db, idRodada);

            if (emails.Count == 0) return;

            Msg("Emails Localizados - " + emails.Count().ToString());
            int contador = 0;
            foreach (var email in emails)
            {
                ValidarEmails(email);
                if (string.IsNullOrEmpty(email.emailAluno) && string.IsNullOrEmpty(email.emailResponsaveis))
                    continue;

                if (string.IsNullOrEmpty(email.emailAluno))
                {
                    string[] emailsResp = email.emailResponsaveis.Split(',');
                    email.emailAluno = emailsResp[0];
                    if (emailsResp.Count() == 1)
                        email.emailResponsaveis = string.Empty;
                    else
                        email.emailResponsaveis = string.Join(",", emailsResp.Skip(1));
                }
                // quando for para testar
                //email.emailAluno = "decmaster10@hotmail.com"; email.emailResponsaveis = "";// = "clarissarotina@gmail.com, clalippi@yahoo.com";
                string message = string.Format(templateEmail, email.nomeAluno, email.RA, email.nomeEscola, DadosEscola(email.emailEscola, email.telefoneEscola));
                try
                {
                    if (!Util.Mail.EnviarEmailBodyHtmlGmail(email.emailAluno, email.emailResponsaveis, string.Concat("Matrícula - Aluno  - ", email.nomeAluno), message))
                    {
                        Msg("Houve um problema com o envio de email. " + email.RA.ToString());
                    }
                    contador += 1;
                }
                catch (Exception ex)
                {
                    Msg("Houve um problema com o envio de email. " + ex.InnerException.ToString());
                }
            }
            Msg("Emails enviados - " + contador);

        }

        /// <summary>
        /// Monta o corpo do email conforme os dados da escola
        /// </summary>
        /// <param name="emailEscola"></param>
        /// <param name="telefoneEscola"></param>
        /// <returns></returns>
        private static string DadosEscola(string emailEscola, string telefoneEscola)
        {
            string mensagem = "";
            if (!string.IsNullOrEmpty(emailEscola) && !string.IsNullOrEmpty(telefoneEscola))
                mensagem = string.Format(@"Solicitamos que entre em contato com sua escola por meio do email <strong>{0}</strong> ou do telefone <strong>{1}</strong> para mais informações sobre as atividades pedagógicas desenvolvidas no período de suspensão física das aulas. ", emailEscola, telefoneEscola);
            else if (!string.IsNullOrEmpty(emailEscola))
                mensagem = string.Format(@"Solicitamos que entre em contato com sua escola por meio do email <strong>{0}</strong> para mais informações sobre as atividades pedagógicas desenvolvidas no período de suspensão física das aulas. ", emailEscola);
            else if (!string.IsNullOrEmpty(telefoneEscola))
                mensagem = string.Format(@"Solicitamos que entre em contato com sua escola por meio do telefone <strong>{0}</strong> para mais informações sobre as atividades pedagógicas desenvolvidas no período de suspensão física das aulas. ", telefoneEscola);
            else
                mensagem = "Solicitamos que entre em contato com sua escola para mais informações sobre as atividades pedagógicas desenvolvidas no período de suspensão física das aulas. ";

            return mensagem;
        }

        /// <summary>
        /// Busca os dados dos alunos matriculados para enviar email
        /// </summary>
        /// <param name="db"></param>
        /// <param name="idRodada"></param>
        /// <returns></returns>
        private static List<Email> BuscarEmails(IDataBase db, int idRodada)
        {
            List<Email> emails = new List<Email>();
            db.ClearParameters();
            db.AddParameter("@ID_RODADA", idRodada, DbType.Int32);
            using (SedDataReader reader = db.ExecuteSedReaderCommandText(Query.QueryAlunosMatriculados))
            {
                while (reader.Read())
                {
                    emails.Add(
                        new Email(
                        reader.GetStringNonDBNull(0, "").Trim(),
                        reader.GetStringNonDBNull(1, "").Trim(),
                        reader.GetStringNonDBNull(2, "").Trim(),
                        reader.GetStringNonDBNull(3, "").Trim(),
                        reader.GetStringNonDBNull(4, "").Trim(),
                        reader.GetStringNonDBNull(5, "").Trim(),
                        reader.GetStringNonDBNull(6, "").Trim(),
                        reader.GetStringNonDBNull(7, "").Trim(),
                        reader.GetStringNonDBNull(8, "").Trim())
                        { }
                    );
                }
            }
            return emails;
        }

        /// <summary>
        /// Validação de Emails
        /// </summary>
        /// <param name="email"></param>
        private static void ValidarEmails(Email email)
        {
            if (!string.IsNullOrEmpty(email.emailAluno) && !EmailValido(email.emailAluno))
            {
                email.emailAluno = string.Empty;
            }

            if (!string.IsNullOrEmpty(email.emailResponsaveis))
            {
                string[] emailsResp = email.emailResponsaveis.Split(',');
                string emailCorreto = string.Empty;

                for (int i = 0; i < emailsResp.Count(); i++)
                {
                    if (EmailValido(emailsResp[i]))
                        emailCorreto = string.Concat(emailsResp[i], ",");
                    i++;
                }
                email.emailResponsaveis = string.IsNullOrEmpty(emailCorreto) ? string.Empty : emailCorreto.Substring(0, emailCorreto.Length - 1);
            }
        }

        /// <summary>
        /// Metodo de validação de Email
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        private static bool EmailValido(string email)
        {
            int tamanhoMaximo = 100;
            if (string.IsNullOrEmpty(email)) return false;
            if (email.Length > tamanhoMaximo) return false;

            email = email.Trim().ToUpperInvariant();

            if (!regmail.IsMatch(email)) return false;
            return true;
        }
        #endregion

    }
}