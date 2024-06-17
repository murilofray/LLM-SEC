using See.Sed.GeoApi.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading;

namespace See.Sed.FichaAluno.Compatibiarra.Models
{
    public class Aluno : ObjetoGeocodificado
    {
        public const double LIMITE_PARA_TRACADO_DE_ROTAS = 2100; // Limite de distância (em metros) para não traçar a rota a pé
        public const double LIMITE_PARA_ALOCACAO_DE_IRMAOS = 2000; //2000; //9999999; //2900; // Limite de distância (em metros lineares) entre um aluno e a escola, no caso de alocação de irmãos
        public const double LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO_APE = 2000; //Limite de distância a Pé entre um aluno e a escola, no caso de definição ou inscrição
        //aqui deveria diminuir para 1000 metros lineares, para não fugir muito da distancia a pé
        public const double LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO = 1200; //2000; //9999999; //5000; // Limite de distância (em metros lineares) entre um aluno e a escola, no caso de definição ou inscrição
        public const int UNIDADES_DESEJADAS = 25; //15;
        private static readonly int[] TURNOS_PARA_TENTAR_ALOCAR = new int[] { (int)Turno.Manha, (int)Turno.Intermediario, (int)Turno.Tarde, (int)Turno.Vespertino, (int)Turno.Todos };

        public sealed class Irmao
        {
            // Se apenas um aluno for deficiente, será o Aluno1
            public readonly Aluno Aluno1;
            public readonly Aluno Aluno2;

            public bool Deficiente => Aluno1.Deficiente;

            public Irmao(Aluno aluno1, Aluno aluno2)
            {
                // Se apenas um aluno for deficiente, será o Aluno1
                if (aluno2.Deficiente)
                {
                    Aluno1 = aluno2;
                    Aluno2 = aluno1;
                }
                else
                {
                    Aluno1 = aluno1;
                    Aluno2 = aluno2;
                }
            }
        }

        public sealed class UnidadeCompatibilizada
        {
            public readonly Unidade Unidade;
            public double Distancia;
            public bool DistanciaAPe; // Se for false, Distancia é a distância geodésica
            public bool TemBarreira;
            public string Poligono;
            public int GoogleCodigoRota;

            public UnidadeCompatibilizada(Unidade unidade, double distancia)
            {
                Unidade = unidade;
                Distancia = distancia;
                GoogleCodigoRota = (int)CodigoRota.Pendente;
            }

            public override string ToString()
            {
                return $"{Distancia.ToString(System.Globalization.CultureInfo.InvariantCulture)} metros ({(DistanciaAPe ? "caminhando" : "linha reta")}) {Unidade.ToString()}";
            }

            public void Serializar(System.IO.BinaryWriter writer)
            {
                writer.Write(Unidade.CodigoUnidadeCorrigido);
                writer.Write(Distancia);
                writer.Write(DistanciaAPe);
                writer.Write(TemBarreira);
                writer.Write(GoogleCodigoRota);
                writer.Write(Poligono ?? "");
            }

            public static UnidadeCompatibilizada Deserializar(System.IO.BinaryReader reader, Dictionary<int, Unidade> unidades)
            {
                // Pode ser que a unidade tenha sumido do banco da execução anterior para essa
                Unidade unidade;
                unidades.TryGetValue(reader.ReadInt32(), out unidade);
                UnidadeCompatibilizada unidadeCompatibilizada = new UnidadeCompatibilizada(unidade, reader.ReadDouble())
                {
                    DistanciaAPe = reader.ReadBoolean(),
                    TemBarreira = reader.ReadBoolean(),
                    GoogleCodigoRota = reader.ReadInt32(),
                    Poligono = reader.ReadString()
                };
                return (unidade == null ? null : unidadeCompatibilizada);
            }
        }

        public readonly int Codigo;
        public readonly int CodigoFicha; // Se for 0, era um aluno de continuidade
        public readonly int FaseFicha; // Se for 0, era um aluno de continuidade    
        public readonly string Nome, RA;
        public bool Deficiente, InteresseNoturno, InteresseEspanhol, InteresseIntegral;
        public readonly int TipoEnsinoDesejado, SerieDesejada, TurnoDesejado;
        public readonly DictionaryRankeado<int, UnidadeCompatibilizada> UnidadesCompatibilizadas;
        private List<UnidadeCompatibilizada> UnidadesCandidatas;
        // Os atributos de irmão devem ser serializados a parte, por causa do formato do aquivo original
        private readonly Dictionary<int, Aluno> Irmaos;
        public bool Gemeo;

        // No caso de definição/inscrição, EscolaDeOrigem e UnidadeDeOrigemCorrigido armazenam
        // a escola/unidade onde o aluno foi definido/inscrito
        public readonly int EscolaDeOrigem, UnidadeDeOrigemCorrigido;

        // Apenas para os alunos de continuidade
        public readonly int TurmaDeOrigem, TurnoDeOrigem, DuracaoDeOrigem;
        public readonly string IdTurmaDeOrigem;
        public readonly string SalaDeOrigem;
        public int CodigoDNEAluno;
        public long CodigoEndereco;
        public readonly int CodigoMotivoFase8;
        public readonly long CodigoMatriculaAnterior;
        public readonly int CodigoEscolaAnterior;
        public readonly int CodigoUnidadeAnterior;

        // Não precisa serializar
        private int UnidadesInicialmenteSemVagasNasUnidadesCandidatas;
        public bool Permaneceu, Congelado, RodadasEtapa1, AlunoFakeApenasParaSerIrmao, DeslocamentoPorEndereco, ForcarUtilizacaoDasCoordenadasDaEscolaDeOrigemPorFaltaDeEscolas;
        public int Ordem;
        public Motivo Motivo;
        public string MotivoStr
        {
            get
            {
                switch (Motivo)
                {
                    case Motivo.Continuidade: return "Continuidade";
                    case Motivo.Definicao: return "Definição";
                    case Motivo.DefinicaoContinuidade: return "Definição-Continuidade";
                    case Motivo.Inscricao: return "Inscrição";
                    case Motivo.ManterJuntoComIrmaoDeContinuidade: return "Manter junto com irmãos de continuidade";
                    case Motivo.ManterJuntoComIrmaos: return "Manter junto com irmãos";
                    case Motivo.SemVagasNaUnidadeDeContinuidade: return "Sem vagas na unidade de continuidade";
                    case Motivo.SemVagasNaUnidadeDeContinuidadeMunicipal: return "Sem vagas na unidade de continuidade municipal";
                    case Motivo.SemUnidadesCompatibilizadas: return "Sem unidades compatibilizadas";
                    case Motivo.SemUnidadesDentroDoLimiteDeDistancia: return "Sem unidades dentro do limite de distância";
                    case Motivo.SemVagasNasUnidadesDentroDoLimiteDeDistancia: return "Sem vagas nas unidades dentro do limite de distância";
                    case Motivo.CEU: return "CEU";
                    case Motivo.PadreTicao: return "Padre Ticão";
                    case Motivo.SemVagasCEU: return "Sem vagas CEU";
                    case Motivo.SemUnidadesCompatibilizadasPadreTicao: return "Sem unidades compatibilizadas (Padre Ticão)";
                    case Motivo.SemUnidadesDentroDoLimiteDeDistanciaPadreTicao: return "Sem unidades dentro do limite de distância (Padre Ticão)";
                    case Motivo.SemVagasNasUnidadesDentroDoLimiteDeDistanciaPadreTicao: return "Sem vagas nas unidades dentro do limite de distância (Padre Ticão)";
                    case Motivo.Congelamento: return "Congelamento";
                    case Motivo.DefinicaoIntegral: return "Definição - Ensino Integral";
                    case Motivo.InscricaoIntegral: return "Inscrição - Ensino Integral";
                    case Motivo.SemGeo: return "Sem geo / Geo inválida";
                    case Motivo.InscricaoDeslocamentoSemEndereco: return "Inscrição por deslocamento sem alteração de endereço";
                    case Motivo.InscricaoDeslocamentoComEndereco: return "Inscrição por deslocamento com alteração de endereço";
                    case Motivo.InscricaoDeslocamentoSemEnderecoIntegral: return "Inscrição por deslocamento sem alteração de endereço - Ensino Integral";
                    case Motivo.InscricaoDeslocamentoComEnderecoIntegral: return "Inscrição por deslocamento com alteração de endereço - Ensino Integral";
                    case Motivo.SemVagaNaUnidadeDeIntencao: return "Sem Vaga na Unidade de Intenção";
                    default: return "Sem geo ou sem ficha";
                }
            }
        }
        public Motivo MotivoBasicoCasoHajaAlocacao
        {
            get
            {
                return (Definicao ?
                    Motivo.Definicao : (Continuidade ?
                        Motivo.Continuidade : (InscricaoDeslocamentoSemEndereco ?
                            Motivo.InscricaoDeslocamentoSemEndereco : (InscricaoDeslocamentoComEndereco ?
                                Motivo.InscricaoDeslocamentoComEndereco :
                                    Motivo.Inscricao))));
            }
        }
        public Motivo MotivoBasicoCasoHajaAlocacaoIntegral
        {
            get
            {
                return (Definicao ?
                    Motivo.DefinicaoIntegral : (InscricaoDeslocamentoSemEndereco ?
                        Motivo.InscricaoDeslocamentoSemEnderecoIntegral : (InscricaoDeslocamentoComEndereco ?
                            Motivo.InscricaoDeslocamentoComEnderecoIntegral :
                                Motivo.InscricaoIntegral)));
            }
        }

        /// <summary>
        /// Usado na matricula para validaçoes 
        /// </summary>
        public int CodigoTipoExcecao
        {
            get
            {
                switch (FaseFicha)
                {
                    case 0:
                        return 4110;
                    case 8:
                        return 4108;
                    case 9:
                        return 4109;
                    default:
                        return 0;
                }
            }
        }

        public Unidade.Turma TurmaAlocada;
        public Unidade UnidadeAlocada;
        public Unidade UnidadeDeOrigem;
        private int TurnoAlocado
        {
            get
            {
                if (TurmaAlocada != null)
                    return TurmaAlocada.Turno;
                if (TurnoDesejado > (int)Turno.Todos)
                    return TurnoDesejado;
                return TurnoDeOrigem;
            }
        }

        // Armazena o código da matrícula sendo criada (o ID já foi reservado na sequence,
        // mas a matrícula ainda não foi inserida)
        public long CodigoMatriculaSendoCriada;

        // Dados originais da matrícula que gerou a transferência
        public long CodigoMatriculaAnterior_089;
        public int EscolaAnterior_089, UnidadeAnteriorCorrigido_089;

        public static string NormalizarRA(string ra)
        {
            return ra.Trim().PadLeft(12, '0');
        }

        public static int ProximaDuracao(int duracaoAtual)
        {
            switch (duracaoAtual)
            {
                case 1:
                    return 2; // Depois do 1o vem o 2o semestre
                case 2:
                    return 1; // Depois do 2o, volta para o 1o semestre
            }
            return 0; // Anual é sempre anual
        }

        public static int SerieContinuidade(ref int tipoEnsinoAtual, int serieAtual, int duracaoAtual)
        {
            // Só podemos compatibilizar alunos de continuidade de turmas anuais ou de 2o semestre
            if (duracaoAtual != 0 && duracaoAtual != 2)
                return -1;
            switch (tipoEnsinoAtual)
            {
                case 6: // ENSINO INFANTIL (5,6,7,1,2)
                    switch (serieAtual)
                    {
                        case 5:
                            return 6;
                        case 6:
                            return 7;
                        case 7:
                            return 1;
                        case 1:
                            return 2;
                    }
                    break;
                case 14: // ENSINO FUNDAMENTAL DE 9 ANOS (1-9)
                    if (serieAtual >= 1 && serieAtual <= 8)
                        return serieAtual + 1;
                    break;
                case 2: // ENSINO MEDIO (1-3)
                case 5: // EJA ENSINO MEDIO (1-3)
                case 50: // ENSINO MEDIO - N3 PRTE  (1-3)
                case 75: // EJA ENSINO MEDIO - TELECURSO PRESENCIAL (1-3)
                    if (serieAtual >= 1 && serieAtual <= 2)
                        return serieAtual + 1;
                    break;
                case 3: // EJA FUNDAMENTAL - ANOS INICIAIS (1-2; 1-4)
                    if (duracaoAtual == 0)
                    {
                        // 1-2 anual
                        if (serieAtual == 1)
                            return 2;
                    }
                    else
                    {
                        // 1-4 semestral
                        if (serieAtual >= 1 && serieAtual <= 3)
                            return serieAtual + 1;
                    }
                    break;
                case 4: // EJA FUNDAMENTAL - ANOS FINAIS (1-4; 13-14)
                    if ((serieAtual >= 1 && serieAtual <= 3) || serieAtual == 13)
                        return serieAtual + 1;
                    break;
                case 30: // ENSINO FUNDAMENTAL - N1 PRTE (1-5)
                    if (serieAtual >= 1 && serieAtual <= 4)
                        return serieAtual + 1;
                    break;
                case 40: // ENSINO FUNDAMENTAL - N2 PRTE (6-9)
                    if (serieAtual >= 6 && serieAtual <= 8)
                        return serieAtual + 1;
                    break;
                case 36: // PROEJA - ENSINO FUNDAMENTAL (9-12)
                    if (serieAtual >= 9 && serieAtual <= 11)
                        return serieAtual + 1;
                    break;
                case 37: // PROEJA - ENSINO MÉDIO (9-11)
                    if (serieAtual >= 9 && serieAtual <= 10)
                        return serieAtual + 1;
                    break;
                case 74: // EJA FUNDAMENTAL - ANOS FINAIS - TELECURSO PRESENCIAL (1-4)
                case 76: // ENSINO MEDIO - VENCE (1-4)
                    if (serieAtual >= 1 && serieAtual <= 3)
                        return serieAtual + 1;
                    break;
                case 78: // ENSINO FUNDAMENTAL 9 ANOS - RC (3)
                case 80: // ENSINO FUNDAMENTAL 9 ANOS - RCI (3)
                    tipoEnsinoAtual = 14; // Recuperação volta para o ensino normal
                    return 4;
                case 81: // ENSINO FUNDAMENTAL 9 ANOS - RC (6)
                case 83: // ENSINO FUNDAMENTAL 9 ANOS - RCI (6)
                    tipoEnsinoAtual = 14; // Recuperação volta para o ensino normal
                    return 7;
            }
            return -1;
        }

        //public static Aluno AlunoDeContinuidade(Dictionary<int, Unidade> unidades, int codigo, string nome, string ra, bool deficiente, int tipoEnsinoAtual, int serieAtual, int escolaDeOrigem, int unidadeDeOrigem, int turmaDeOrigem, int turnoDeOrigem, int duracaoDeOrigem, string idTurmaDeOrigem, string salaDeOrigem, int codigoDNEAluno)
        //{
        //    int proximoTipoEnsino = tipoEnsinoAtual;
        //    int proximaSerie = SerieContinuidade(ref proximoTipoEnsino, serieAtual, duracaoDeOrigem);
        //    // Algo saiu errado com os dados
        //    if (proximaSerie < 0)
        //        return null;
        //    return new Aluno(unidades, codigo, 0, 0, nome, ra, deficiente, false, false, false, proximoTipoEnsino, proximaSerie, turnoDeOrigem, double.PositiveInfinity, double.PositiveInfinity, escolaDeOrigem, unidadeDeOrigem, turmaDeOrigem, turnoDeOrigem, duracaoDeOrigem, idTurmaDeOrigem.Trim().ToUpper(), salaDeOrigem.Trim().ToUpper());
        //}

        public static Aluno IrmaoFakeDeContinuidade(Dictionary<int, Unidade> unidades, int codigo, string nome, string ra, bool deficiente, int tipoEnsinoAtual, int serieAtual, int escolaDeOrigem, int unidadeDeOrigem, int turmaDeOrigem, int turnoDeOrigem, int duracaoDeOrigem, string idTurmaDeOrigem, string salaDeOrigem, Unidade unidadeAlocada, Unidade.Turma turmaAlocada)
        {
            return new Aluno(unidades, codigo, 0, 0, nome, ra, deficiente, false, false, false, tipoEnsinoAtual, serieAtual, turnoDeOrigem, double.PositiveInfinity, double.PositiveInfinity, escolaDeOrigem, unidadeDeOrigem, turmaDeOrigem, turnoDeOrigem, duracaoDeOrigem, idTurmaDeOrigem.Trim().ToUpper(), salaDeOrigem.Trim().ToUpper(), 0, 0, 0,0,0,0)
            {
                AlunoFakeApenasParaSerIrmao = true,
                UnidadeAlocada = unidadeAlocada,
                TurmaAlocada = turmaAlocada,
                Motivo = Motivo.Continuidade
            };
        }

        public static Aluno AlunoDeDefinicaoInscricao(Dictionary<int, Unidade> unidades, int codigo, int codigoFicha, int faseFicha, string nome, string ra, bool deficiente, bool interesseNoturno, bool interesseEspanhol, bool interesseIntegral, int tipoEnsinoDesejado, int serieDesejada, int turnoDesejado, double latitude, double longitude, int escolaDeOrigem, int unidadeDeOrigem, int codigoDNEAluno, long codigoEndereco, int codigoMotivoFase8, long codigoMatriculaAnterior, int codigoEscolaAnterior, int codigoUnidadeAnterior)
        {
            // A regra diz que devemos tratar apenas 77, 78, 79, 80, 81, 82, 83, 84...
            // Será que não seria melhor usar todo tipo de equivalência???
            switch (tipoEnsinoDesejado)
            {
                case 1: // ENSINO FUNDAMENTAL
                        // Séries de 1 a 8 viram 2 a 9
                    tipoEnsinoDesejado = 14;
                    serieDesejada++;
                    break;
                case 76: // ENSINO MEDIO - VENCE
                    tipoEnsinoDesejado = 2;
                    if (serieDesejada > 3)
                        serieDesejada = 3;
                    break;
                case 77: // ENSINO FUNDAMENTAL-RC
                case 79: // ENSINO FUNDAMENTAL-RCI
                         // São a recuperação do ENSINO FUNDAMENTAL
                    tipoEnsinoDesejado = 14;
                    serieDesejada++;
                    break;
                case 78: // ENSINO FUNDAMENTAL 9 ANOS - RC
                case 80: // ENSINO FUNDAMENTAL 9 ANOS - RCI
                case 81: // ENSINO FUNDAMENTAL 9 ANOS - RC
                case 82: // ENSINO FUNDAMENTAL 9 ANOS - RC
                case 83: // ENSINO FUNDAMENTAL 9 ANOS - RCI
                case 84: // ENSINO FUNDAMENTAL 9 ANOS - RCI
                    tipoEnsinoDesejado = 14;
                    break;
            }
            return new Aluno(unidades, codigo, codigoFicha, faseFicha, nome, ra, deficiente, interesseNoturno, interesseEspanhol, interesseIntegral, tipoEnsinoDesejado, serieDesejada, turnoDesejado, latitude, longitude, escolaDeOrigem, unidadeDeOrigem, 0, 0, 0, null, null, codigoDNEAluno, codigoEndereco, codigoMotivoFase8, codigoMatriculaAnterior, codigoEscolaAnterior, codigoUnidadeAnterior);
        }

        private Aluno(Dictionary<int, Unidade> unidades, int codigo, int codigoFicha, int faseFicha, string nome, string ra, bool deficiente, bool interesseNoturno, bool interesseEspanhol, bool interesseIntegral, int tipoEnsinoDesejado, int serieDesejada, int turnoDesejado, double latitude, double longitude, int escolaDeOrigem, int unidadeDeOrigem, int turmaDeOrigem, int turnoDeOrigem, int duracaoDeOrigem, string idTurmaDeOrigem, string salaDeOrigem, int codigoDNEAluno, long codigoEndereco, int codigoMotivoFase8,long codigoMatriculaAnterior, int codigoEscolaAnterior, int codigoUnidadeAnterior) : base(new Coordenada(latitude, longitude))
        {
            if (codigoFicha == 0)
            {
                // Ajusta o dado de turno desejado, visto que alunos de Definição/Inscrição
                // só escolhem se querem ficar no integral ou no noturno, não existe outra
                // opção de entrada
                if (interesseIntegral)
                    turnoDesejado = (int)Turno.Integral;
                else if (interesseNoturno)
                    turnoDesejado = (int)Turno.Noite;
                else if (turnoDesejado == (int)Turno.Integral)
                    interesseIntegral = true;
                else if (turnoDesejado == (int)Turno.Noite)
                    interesseNoturno = true;
                else
                    turnoDesejado = (int)Turno.Todos;
            }

            Codigo = codigo;
            CodigoFicha = codigoFicha;
            FaseFicha = faseFicha;
            Nome = nome;
            RA = NormalizarRA(ra ?? "");
            Deficiente = deficiente;
            InteresseNoturno = interesseNoturno;
            InteresseEspanhol = interesseEspanhol;
            InteresseIntegral = interesseIntegral;
            TipoEnsinoDesejado = tipoEnsinoDesejado;
            SerieDesejada = serieDesejada;
            TurnoDesejado = turnoDesejado;
            UnidadesCompatibilizadas = new DictionaryRankeado<int, UnidadeCompatibilizada>(UNIDADES_DESEJADAS);
            Irmaos = new Dictionary<int, Aluno>();

            if (unidadeDeOrigem == 0 && escolaDeOrigem != 0)
            {
                // Se não veio a unidade, utiliza uma qualquer (caso raro, apenas
                // para alunos de definição/inscrição com bug no registro)
                foreach (Unidade unidade in unidades.Values)
                {
                    if (unidade.CodigoEscola == escolaDeOrigem)
                    {
                        unidadeDeOrigem = unidade.CodigoUnidadeCorrigido;
                        break;
                    }
                }
            }
            EscolaDeOrigem = escolaDeOrigem;
            UnidadeDeOrigemCorrigido = Unidade.CorrigirCodigoUnidade(escolaDeOrigem, unidadeDeOrigem);
            unidades.TryGetValue(UnidadeDeOrigemCorrigido, out UnidadeDeOrigem);

            CodigoDNEAluno = codigoDNEAluno;
            CodigoEndereco = codigoEndereco;
            CodigoMatriculaAnterior = codigoMatriculaAnterior;
            CodigoEscolaAnterior = codigoEscolaAnterior;
            CodigoUnidadeAnterior = codigoUnidadeAnterior;


            // Apenas para os alunos de continuidade

            TurmaDeOrigem = turmaDeOrigem;
            TurnoDeOrigem = turnoDeOrigem;
            DuracaoDeOrigem = duracaoDeOrigem;
            IdTurmaDeOrigem = idTurmaDeOrigem;
            SalaDeOrigem = salaDeOrigem;
            CodigoMotivoFase8 = codigoMotivoFase8;
            
        }

        public override string ToString()
        {
            return $"A {Codigo} ({Coordenada.ToString()})";
        }

        public bool JaAlocado => (UnidadeAlocada != null);

        public bool Continuidade => (CodigoFicha == 0);

        // Durante as rodadas, a fase de definição 1 deve ser considerada como inscrição externa
        public bool Definicao => (Program.Rodadas ? (FaseFicha == 2 || FaseFicha == 6) : (FaseFicha == 1 || FaseFicha == 2 || FaseFicha == 6));

        public bool InscricaoDeslocamentoComOuSemEndereco => (FaseFicha == 0 || FaseFicha == 8 || FaseFicha == 9);

        public bool InscricaoDeslocamentoSemEndereco => (FaseFicha == 9 || (FaseFicha == 8 && !DeslocamentoPorEndereco));

        public bool InscricaoDeslocamentoComEndereco => (FaseFicha == 0 || (FaseFicha == 8 && DeslocamentoPorEndereco));

        public bool TemIrmaoAlocadoNaMesmaTurma
        {
            get
            {
                if (TurmaAlocada != null)
                {
                    foreach (Aluno aluno in Irmaos.Values)
                    {
                        if (TurmaAlocada == aluno.TurmaAlocada)
                            return true;
                    }
                }
                return false;
            }
        }

        public void Serializar(System.IO.BinaryWriter writer)
        {
            writer.Write(Codigo);
            writer.Write(CodigoFicha);
            writer.Write(FaseFicha);
            writer.Write(Nome ?? "");
            writer.Write(RA ?? "");
            writer.Write(Deficiente);
            writer.Write(InteresseNoturno);
            writer.Write(InteresseEspanhol);
            writer.Write(InteresseIntegral);
            writer.Write(TipoEnsinoDesejado);
            writer.Write(SerieDesejada);
            writer.Write(TurnoDesejado);
            writer.Write(Coordenada.Latitude);
            writer.Write(Coordenada.Longitude);

            writer.Write(EscolaDeOrigem);
            writer.Write(UnidadeDeOrigemCorrigido);

            if (Continuidade)
            {
                writer.Write(TurmaDeOrigem);
                writer.Write(TurnoDeOrigem);
                writer.Write(DuracaoDeOrigem);
                writer.Write(IdTurmaDeOrigem);
                writer.Write(SalaDeOrigem);
            }

            writer.Write(UnidadesCompatibilizadas.Quantidade);
            // A ordem é importante!
            foreach (UnidadeCompatibilizada unidade in UnidadesCompatibilizadas)
                unidade.Serializar(writer);
        }

        public static Aluno Deserializar(System.IO.BinaryReader reader, Dictionary<int, Unidade> unidades)
        {
            int codigo = reader.ReadInt32(), codigoFicha = reader.ReadInt32(), faseFicha = reader.ReadInt32();

            Aluno aluno = (codigoFicha == 0 ?
                // Continuidade
                new Aluno(unidades, codigo, codigoFicha, faseFicha, reader.ReadString(), reader.ReadString(), reader.ReadBoolean(), reader.ReadBoolean(), reader.ReadBoolean(), reader.ReadBoolean(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadString(), reader.ReadString(), 0, 0, 0,0,0,0)
                :
                // Definição/Inscrição
                new Aluno(unidades, codigo, codigoFicha, faseFicha, reader.ReadString(), reader.ReadString(), reader.ReadBoolean(), reader.ReadBoolean(), reader.ReadBoolean(), reader.ReadBoolean(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadDouble(), reader.ReadDouble(), reader.ReadInt32(), reader.ReadInt32(), 0, 0, 0, null, null, 0, 0, 0,0,0,0)
                );
            int total = reader.ReadInt32();
            for (int i = total - 1; i >= 0; i--)
            {
                // Pode ser que a unidade tenha sumido do banco da execução anterior para essa
                UnidadeCompatibilizada unidade = UnidadeCompatibilizada.Deserializar(reader, unidades);
                if (unidade == null)
                    continue;
                // A ordem é importante!
                aluno.UnidadesCompatibilizadas.Adicionar(unidade.Unidade.CodigoUnidadeCorrigido, unidade);
            }
            aluno.UnidadesCompatibilizadas.Ordenar(ComparadorUnidades);
            return aluno;
        }

        private static int ComparadorUnidades(UnidadeCompatibilizada a, UnidadeCompatibilizada b)
        {
            if (a.TemBarreira != b.TemBarreira)
                return (a.TemBarreira ? 1 : -1);
            return (a.Distancia < b.Distancia ? -1 : (a.Distancia > b.Distancia ? 1 : 0));
        }

        public static bool DecifrarLatLng(bool podeUtilizarEscola, string latEnd, string lngEnd, Dictionary<int, Unidade> unidades, int escolaDeOrigem, int unidadeDeOrigem, out double lat, out double lng)
        {
            //Removendo lat e long Indicativo
            //string latEndIndi, string lngEndIndi,
            //if (!Coordenada.TryParse(latEndIndi, lngEndIndi, out lat, out lng) ||
            //    !LimitesDoEstado.ContemCoordenada(lat, lng))
            //{

            if (!Coordenada.TryParse(latEnd, lngEnd, out lat, out lng) ||
                !LimitesDoEstado.ContemCoordenada(lat, lng))
            {

                if (podeUtilizarEscola && unidades != null)
                {
                    if (unidadeDeOrigem == 0 && escolaDeOrigem != 0)
                    {
                        // Se não veio a unidade, utiliza uma qualquer (caso raro, apenas
                        // para alunos de definição/inscrição com bug no registro)
                        foreach (Unidade unidade in unidades.Values)
                        {
                            if (unidade.CodigoEscola == escolaDeOrigem)
                            {
                                unidadeDeOrigem = unidade.CodigoUnidadeCorrigido;
                                break;
                            }
                        }
                    }

                    Unidade u;
                    unidades.TryGetValue(Unidade.CorrigirCodigoUnidade(escolaDeOrigem, unidadeDeOrigem), out u);
                    if (u == null || !u.CoordenadaValida)
                    {
                        lat = double.PositiveInfinity;
                        lng = double.PositiveInfinity;
                        return false;
                    }
                    lat = u.Coordenada.Latitude;
                    lng = u.Coordenada.Longitude;
                }
                else
                {
                    lat = double.PositiveInfinity;
                    lng = double.PositiveInfinity;
                    return false;
                }
            }
            //}

            return true;
        }

        public void FinalizarAlocacao(Unidade unidade, Unidade.Turma turma, Motivo motivo)
        {
            if (turma != null)
                turma.Alunos[Codigo] = this;
            UnidadeAlocada = unidade;
            TurmaAlocada = turma;
            Motivo = motivo;
        }

        public void AdicionarUnidadeCompatibilizada(Unidade unidade, double distancia)
        {
            //limpa antes de adicionar a nova escola.
            UnidadesCompatibilizadas.Limpar();

            if (unidade == null) return;
            var unidadeCompatibilizada = new UnidadeCompatibilizada(unidade, distancia);
            unidadeCompatibilizada.DistanciaAPe = true;
            unidadeCompatibilizada.GoogleCodigoRota = (int)CodigoRota.OK;

            UnidadesCompatibilizadas.Adicionar(unidade.CodigoUnidadeCorrigido, unidadeCompatibilizada);
        }

        #region Definição/Inscrição

        public void PrepararTesteUnidades()
        {
            UnidadesCandidatas = new List<UnidadeCompatibilizada>();
        }

        public bool TestarUnidades(IEnumerable<Unidade> loteDeUnidades, bool ensinoIntegral, bool deixarExcedentesEnsinoIntegral)
        {
            // Dessa forma nós retornamos false apenas na "próxima execução" depois da final
            bool executando = (UnidadesCandidatas.Count < UNIDADES_DESEJADAS);

            // Esse método assume que as unidades estão sendo fornecidas
            // em lotes, sendo que as unidades de cada lote têm distâncias
            // maiores que as distâncias das unidades dos lotes anteriores
            foreach (Unidade unidade in loteDeUnidades)
            {
                //Só pode compatibilizar nas unidades de mesma DNE
                if (unidade.CodigoMunicipio != CodigoDNEAluno) continue;

                if (unidade.InicialmenteTemVagasParaTipoEnsinoSerie(TipoEnsinoDesejado, SerieDesejada, ensinoIntegral))
                    UnidadesCandidatas.Add(new UnidadeCompatibilizada(unidade,
                        ForcarUtilizacaoDasCoordenadasDaEscolaDeOrigemPorFaltaDeEscolas ?
                        UnidadeDeOrigem.DistanciaGeodesica(unidade) :
                        DistanciaGeodesica(unidade)
                        ));
                else
                    UnidadesInicialmenteSemVagasNasUnidadesCandidatas++;
            }

            // Se com esse lote de escolas já atingimos/passamos da quantidade
            // desejada, basta ordenar e retornar false, para dizer que acabamos
            if (!executando)
            {
                UnidadesCandidatas.Sort((a, b) =>
                {
                    return ((a.Distancia < b.Distancia) ? -1 : ((a.Distancia > b.Distancia) ? 1 : 0));
                });

                // Remove o excesso de unidades
                if (UnidadesCandidatas.Count > UNIDADES_DESEJADAS)
                {
                    if (!deixarExcedentesEnsinoIntegral)
                    {
                        UnidadesCandidatas.RemoveRange(UNIDADES_DESEJADAS, UnidadesCandidatas.Count - UNIDADES_DESEJADAS);
                    }
                    else
                    {
                        // Remove o excesso, mas deixa as escolas com ensino integral
                        for (int i = UnidadesCandidatas.Count - 1; i >= UNIDADES_DESEJADAS; i--)
                        {
                            if (!UnidadesCandidatas[i].Unidade.InicialmenteTemVagasParaTipoEnsinoSerie(TipoEnsinoDesejado, SerieDesejada, true))
                                UnidadesCandidatas.RemoveAt(i);
                        }
                    }
                }
                return false;
            }

            return executando;
        }

        public bool TerminarTesteUnidades()
        {
            // Nós vamos efetivamente ficar com as unidades em UnidadesCandidatas,
            // mas precisamos reaproveitar os objetos em UnidadesCompatibilizadas,
            // pois eles podem já ter sido calculados
            for (int i = UnidadesCandidatas.Count - 1; i >= 0; i--)
            {
                UnidadeCompatibilizada unidadeAntiga;
                if (UnidadesCompatibilizadas.ValorDaChave(UnidadesCandidatas[i].Unidade.CodigoUnidadeCorrigido, out unidadeAntiga))
                    UnidadesCandidatas[i] = unidadeAntiga;
            }
            // Agora substitui as antigas pelas novas (pode ter mudado muitas,
            // todas, ou nenhuma...)
            UnidadesCompatibilizadas.Limpar();
            for (int i = UnidadesCandidatas.Count - 1; i >= 0; i--)
            {
                UnidadeCompatibilizada unidade = UnidadesCandidatas[i];
                UnidadesCompatibilizadas.Adicionar(unidade.Unidade.CodigoUnidadeCorrigido, unidade);
            }
            UnidadesCompatibilizadas.Ordenar(ComparadorUnidades);
            UnidadesCandidatas.Clear();
            UnidadesCandidatas = null;
            // Retorna true se existir ao menos uma unidade compatibilizada dentro dos
            // limite de distância estipulado (pode comparar apenas a distância da unidade
            // em ValorDoRank(0), pois ValorDoRank(0) é a mais próxima)
            return (UnidadesCompatibilizadas.Quantidade > 0 &&
                UnidadesCompatibilizadas.ValorDoRank(0).Distancia <= (UnidadesCompatibilizadas.ValorDoRank(0).DistanciaAPe ? LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO_APE : LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO));
        }

        public bool SemUnidadesCompatibilizadas => (UnidadesCompatibilizadas.Quantidade == 0);

        public int UnidadesCompatibilizadasAindaSemGoogle
        {
            get
            {
                int total = 0;
                foreach (UnidadeCompatibilizada unidade in UnidadesCompatibilizadas)
                {
                    if (unidade.Distancia <= LIMITE_PARA_TRACADO_DE_ROTAS)
                    {
                        switch ((CodigoRota)unidade.GoogleCodigoRota)
                        {
                            case CodigoRota.ErroEntradaAlunoInvalida:
                            case CodigoRota.ErroEntradaEscolaInvalida:
                            case CodigoRota.ErroRotaZerada:
                            case CodigoRota.ErroEntradaInvalida:
                            case CodigoRota.NaoEncontrada:
                            case CodigoRota.RotaInexistente:
                            case CodigoRota.OK:
                                continue;
                        }
                        total++;
                    }
                }
                return total;
            }
        }

        public void CalcularGoogle(ref int totalErro, ref int totalGoogle, ref int minGoogle, ref int maxGoogle)
        {
            foreach (UnidadeCompatibilizada unidade in UnidadesCompatibilizadas)
            {
                if (unidade.Distancia <= LIMITE_PARA_TRACADO_DE_ROTAS)
                {
                    switch ((CodigoRota)unidade.GoogleCodigoRota)
                    {
                        case CodigoRota.ErroEntradaAlunoInvalida:
                        case CodigoRota.ErroEntradaEscolaInvalida:
                        case CodigoRota.ErroRotaZerada:
                        case CodigoRota.ErroEntradaInvalida:
                        case CodigoRota.NaoEncontrada:
                        case CodigoRota.RotaInexistente:
                            // Dados inválidos na execução original, apenas substitui
                            // a distância e a flag (aqui precisa substituir a flag!)
                            unidade.Distancia = DistanciaGeodesica(unidade.Unidade);
                            unidade.DistanciaAPe = false;
                            continue;
                        case CodigoRota.OK:
                            // Nada a fazer!
                            continue;
                            // Nesses dois casos, devemos tentar novamente (ou pela primeira vez)
                            //case CodigoRota.ErroTenteNovamente:
                            //case CodigoRota.Pendente:
                    }

                    int antes = Environment.TickCount;

                    ResultadoRotaAlunoEscola r = new ResultadoRotaAlunoEscola();

                    unidade.GoogleCodigoRota = (int)GeoApi.ServicoDeRotas.TracarRotaAlunoEscola(Coordenada.Latitude, Coordenada.Longitude,
                        unidade.Unidade.Coordenada.Latitude,
                        unidade.Unidade.Coordenada.Longitude,
                        "seduc_rota_compat_2018", r, false);

                    unidade.Distancia = r.DistanciaRotaEfetiva;
                    unidade.TemBarreira = r.ExisteBarreiraCaminhando;
                    unidade.Poligono = r.PontosRotaEfetiva ?? r.PontosRotaCaminhando;
                    unidade.DistanciaAPe = true;

                    int delta = Environment.TickCount - antes;

                    if (unidade.GoogleCodigoRota == (int)CodigoRota.ErroTenteNovamente) totalErro++;
                    totalGoogle++;
                    if (minGoogle == 0 || delta < minGoogle) minGoogle = delta;
                    if (delta > maxGoogle) maxGoogle = delta;

                    if (delta < 150)
                        Thread.Sleep(150 - delta);
                }
            }
        }

        public void InvalidarRotas(HashSet<int> unidadesComGeoAlterada)
        {
            foreach (UnidadeCompatibilizada unidade in UnidadesCompatibilizadas)
            {
                if (unidadesComGeoAlterada.Contains(unidade.Unidade.CodigoUnidadeCorrigido))
                    unidade.GoogleCodigoRota = (int)CodigoRota.Pendente;
            }
        }

        public void PreAlocarNasUnidadesCompatibilizadas()
        {
            // Pré-aloca esse aluno em todas as unidades possíveis próximas dele
            foreach (UnidadeCompatibilizada unidade in UnidadesCompatibilizadas)
                unidade.Unidade.AlunosPossiveisPorDistancia.Add(this);
        }

        public double DistanciaAteUnidade(Unidade unidade)
        {
            // Se nós fomos compatibilizados com a unidade, utiliza a distância a pé (rota),
            // caso contrário, utiliza a distância linear entre esse aluno e a unidade
            UnidadeCompatibilizada unidadeCompatibilizada;
            if (!UnidadesCompatibilizadas.ValorDaChave(unidade.CodigoUnidadeCorrigido, out unidadeCompatibilizada))
            {
                if (!CoordenadaValida || !unidade.CoordenadaValida)
                    return double.MaxValue;
                return DistanciaGeodesica(unidade);
            }
            return unidadeCompatibilizada.Distancia;
        }

        public bool TentarSeAlocarNaUnidadeDeOrigem(bool rodadas)
        {
            int turnoAlocado;

            // Aluno de Definição tem preferência em continuar na mesma escola
            // que ele está, caso essa escola atenda o tipo de ensino desejado
            // (independentemente da distância)
            if ((Definicao || InscricaoDeslocamentoSemEndereco) && UnidadeDeOrigem != null && UnidadeDeOrigem.TentarAlocarAlunoDefinicaoInscricao(this, false, rodadas, out turnoAlocado, Definicao ? Motivo.DefinicaoContinuidade : Motivo.InscricaoDeslocamentoSemEndereco))
                return true;

            if (InscricaoDeslocamentoSemEndereco)
                Motivo = Motivo.SemVagaNaUnidadeDeIntencao;
            else
                Motivo = Motivo.SemVagasNaUnidadeDeContinuidade;
            return false;
        }

        public bool TentarSeAlocarNaUnidadeDeOrigemComAlteracaoDeEndereco(bool rodadas)
        {
            int turnoAlocado;

            // Aluno de Definição tem preferência em continuar na mesma escola
            // que ele está, caso essa escola atenda o tipo de ensino desejado
            // (independentemente da distância)
            if (InscricaoDeslocamentoComEndereco && UnidadeDeOrigem != null && UnidadeDeOrigem.TentarAlocarAlunoDefinicaoInscricao(this, false, rodadas, out turnoAlocado, Definicao ? Motivo.DefinicaoContinuidade : Motivo.InscricaoDeslocamentoComEndereco))
                return true;

            Motivo = Motivo.SemVagaNaUnidadeDeIntencao;

            return false;
        }

        public bool TentarSeAlocarEmAlgumaUnidadeCompatibilizada(bool rodadas)
        {
            int turnoAlocado;

            // Aluno de Definição tem preferência em continuar na mesma escola
            // que ele está, caso essa escola atenda o tipo de ensino desejado
            // (independentemente da distância)
            if ((Definicao || InscricaoDeslocamentoSemEndereco) && UnidadeDeOrigem != null && UnidadeDeOrigem.TentarAlocarAlunoDefinicaoInscricao(this, false, rodadas, out turnoAlocado, Definicao ? Motivo.DefinicaoContinuidade : Motivo.InscricaoDeslocamentoSemEndereco))
                return true;

            // No caso de alunos integrais, primeiro tenta em todas as escolas, com turno integral,
            // depois tenta o resto
            //REGRA APLICADA NO METODO - TentarSeAlocarEmAlgumaUnidadeCompatibilizadaIntegral
            //if (InteresseIntegral) //  || TurnoDesejado == (int)Turno.Integral
            //{
            //    if (Deficiente)
            //    {
            //        // Para alunos deficientes, primeiro tentar alocar em alguma
            //        // unidade com acessibilidade, e se não for possível, em qualquer
            //        // outra unidade
            //        foreach (UnidadeCompatibilizada unidade in UnidadesCompatibilizadas)
            //        {
            //            // Ensino integral não tem limite de distância!
            //            //if (//unidade.Distancia <= LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO &&
            //            // Antes tinha, depois não tinha, agora tem!
            //            if (unidade.Distancia <= (unidade.DistanciaAPe ? LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO_APE : LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO) &&
            //                unidade.Unidade.Acessivel &&
            //                unidade.Unidade.TentarAlocarAlunoDefinicaoInscricao(this, false, rodadas, out turnoAlocado, MotivoBasicoCasoHajaAlocacaoIntegral, (int)Turno.Integral))
            //                return true;
            //        }
            //    }
            //    foreach (UnidadeCompatibilizada unidade in UnidadesCompatibilizadas)
            //    {
            //        // Ensino integral não tem limite de distância!
            //        //if (//unidade.Distancia <= LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO &&
            //        // Antes tinha, depois não tinha, agora tem!
            //        if (unidade.Distancia <= (unidade.DistanciaAPe ? LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO_APE : LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO) &&
            //            unidade.Unidade.TentarAlocarAlunoDefinicaoInscricao(this, false, rodadas, out turnoAlocado, MotivoBasicoCasoHajaAlocacaoIntegral, (int)Turno.Integral))
            //            return true;
            //    }
            //}

            if (Deficiente)
            {
                // Para alunos deficientes, primeiro tentar alocar em alguma
                // unidade com acessibilidade, e se não for possível, em qualquer
                // outra unidade
                foreach (UnidadeCompatibilizada unidade in UnidadesCompatibilizadas)
                {
                    if (unidade.Distancia <= (unidade.DistanciaAPe ? LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO_APE : LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO) &&
                        unidade.Unidade.Acessivel &&
                        unidade.Unidade.TentarAlocarAlunoDefinicaoInscricao(this, false, rodadas, out turnoAlocado, MotivoBasicoCasoHajaAlocacao))
                        return true;
                }
            }
            foreach (UnidadeCompatibilizada unidade in UnidadesCompatibilizadas)
            {
                if (unidade.Distancia <= (unidade.DistanciaAPe ? LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO_APE : LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO) &&
                    unidade.Unidade.TentarAlocarAlunoDefinicaoInscricao(this, false, rodadas, out turnoAlocado, MotivoBasicoCasoHajaAlocacao))
                    return true;
            }

            foreach (UnidadeCompatibilizada unidade in UnidadesCompatibilizadas)
            {
                if (unidade.Distancia <= (unidade.DistanciaAPe ? LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO_APE : LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO))
                {
                    Motivo = Motivo.SemVagasNasUnidadesDentroDoLimiteDeDistancia;
                    return false;
                }
            }

            Motivo = (UnidadesCompatibilizadas.Quantidade > 0 ?
                (UnidadesInicialmenteSemVagasNasUnidadesCandidatas > 0 ?
                    Motivo.SemVagasNasUnidadesDentroDoLimiteDeDistancia :
                    Motivo.SemUnidadesDentroDoLimiteDeDistancia) :
                (CoordenadaValida ?
                    Motivo.SemUnidadesCompatibilizadas :
                    Motivo.SemGeo)
            );

            return false;
        }

        public bool TentarSeAlocarEmAlgumaUnidadeCompatibilizadaIntegral(bool rodadas)
        {
            int turnoAlocado;

            if (Deficiente)
            {
                // Para alunos deficientes, primeiro tentar alocar em alguma
                // unidade com acessibilidade, e se não for possível, em qualquer
                // outra unidade
                foreach (UnidadeCompatibilizada unidade in UnidadesCompatibilizadas)
                {
                    if (unidade.Distancia <= (unidade.DistanciaAPe ? LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO_APE : LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO) &&
                        unidade.Unidade.Acessivel &&
                        unidade.Unidade.TentarAlocarAlunoDefinicaoInscricaoIntegral(this, false, rodadas, out turnoAlocado, MotivoBasicoCasoHajaAlocacaoIntegral, (int)Turno.Integral))
                        return true;
                }
            }
            foreach (UnidadeCompatibilizada unidade in UnidadesCompatibilizadas)
            {
                if (unidade.Distancia <= (unidade.DistanciaAPe ? LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO_APE : LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO) &&
                    unidade.Unidade.TentarAlocarAlunoDefinicaoInscricaoIntegral(this, false, rodadas, out turnoAlocado, MotivoBasicoCasoHajaAlocacaoIntegral, (int)Turno.Integral))
                    return true;
            }

            Motivo = (UnidadesCompatibilizadas.Quantidade > 0 ?
                (UnidadesInicialmenteSemVagasNasUnidadesCandidatas > 0 ?
                    Motivo.SemVagasNasUnidadesDentroDoLimiteDeDistancia :
                    Motivo.SemUnidadesDentroDoLimiteDeDistancia) :
                (CoordenadaValida ?
                    Motivo.SemUnidadesCompatibilizadas :
                    Motivo.SemGeo)
            );
            return false;
        }

        public bool TentarSeAlocarEmAlgumaUnidadeCompatibilizadaPadreTicao(bool rodadas)
        {
            int turnoAlocado;

            // ***** Aluno da Padre Ticão deve ir para alguma escola da rede municipal

            // No caso de alunos integrais, primeiro tenta em todas as escolas, com turno integral,
            // depois tenta o resto
            if (InteresseIntegral) // || TurnoDesejado == (int)Turno.Integral
            {
                if (Deficiente)
                {
                    // Para alunos deficientes, primeiro tentar alocar em alguma
                    // unidade com acessibilidade, e se não for possível, em qualquer
                    // outra unidade
                    foreach (UnidadeCompatibilizada unidade in UnidadesCompatibilizadas)
                    {
                        if (unidade.Distancia <= (unidade.DistanciaAPe ? LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO_APE : LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO) &&
                            // ***** Aluno da Padre Ticão deve ir para alguma escola da rede municipal
                            unidade.Unidade.CodigoRedeEnsino == 2 &&
                            unidade.Unidade.Acessivel &&
                            unidade.Unidade.TentarAlocarAlunoDefinicaoInscricao(this, false, rodadas, out turnoAlocado, Motivo.PadreTicao, (int)Turno.Integral))
                            return true;
                    }
                }
                foreach (UnidadeCompatibilizada unidade in UnidadesCompatibilizadas)
                {
                    if (unidade.Distancia <= (unidade.DistanciaAPe ? LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO_APE : LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO) &&
                        // ***** Aluno da Padre Ticão deve ir para alguma escola da rede municipal
                        unidade.Unidade.CodigoRedeEnsino == 2 &&
                        unidade.Unidade.TentarAlocarAlunoDefinicaoInscricao(this, false, rodadas, out turnoAlocado, Motivo.PadreTicao, (int)Turno.Integral))
                        return true;
                }
            }

            if (Deficiente)
            {
                // Para alunos deficientes, primeiro tentar alocar em alguma
                // unidade com acessibilidade, e se não for possível, em qualquer
                // outra unidade
                foreach (UnidadeCompatibilizada unidade in UnidadesCompatibilizadas)
                {
                    if (unidade.Distancia <= (unidade.DistanciaAPe ? LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO_APE : LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO) &&
                        // ***** Aluno da Padre Ticão deve ir para alguma escola da rede municipal
                        unidade.Unidade.CodigoRedeEnsino == 2 &&
                        unidade.Unidade.Acessivel &&
                        unidade.Unidade.TentarAlocarAlunoDefinicaoInscricao(this, false, rodadas, out turnoAlocado, Motivo.PadreTicao))
                        return true;
                }
            }
            foreach (UnidadeCompatibilizada unidade in UnidadesCompatibilizadas)
            {
                if (unidade.Distancia <= (unidade.DistanciaAPe ? LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO_APE : LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO) &&
                    // ***** Aluno da Padre Ticão deve ir para alguma escola da rede municipal
                    unidade.Unidade.CodigoRedeEnsino == 2 &&
                    unidade.Unidade.TentarAlocarAlunoDefinicaoInscricao(this, false, rodadas, out turnoAlocado, Motivo.PadreTicao))
                    return true;
            }

            foreach (UnidadeCompatibilizada unidade in UnidadesCompatibilizadas)
            {
                if (unidade.Distancia <= (unidade.DistanciaAPe ? LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO_APE : LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO))
                {
                    Motivo = Motivo.SemVagasNasUnidadesDentroDoLimiteDeDistanciaPadreTicao;
                    return false;
                }
            }

            Motivo = (UnidadesCompatibilizadas.Quantidade > 0 ? Motivo.SemUnidadesDentroDoLimiteDeDistanciaPadreTicao : Motivo.SemUnidadesCompatibilizadasPadreTicao);

            return false;
        }

        private bool TentativaDeAlocacaoPadraoDeIrmaos(bool rodadas, List<Aluno> lideres, List<Aluno> seguidores, List<Unidade> unidadesTemp)
        {
            int turnoAlocado;

            // A ideia será: vamos tentar alocar os "seguidores" (irmãos ainda não alocados)
            // nas mesmas unidades dos "líderes" (irmãos já alocados)

            // Tenta alocar todos os seguidores na mesma unidade do líder onde for
            // possível alocar a maior quantidade de seguidores
            // Em seguida, tenta alocar os irmãos restantes na unidade do segundo
            // maior líder, e assim por diante
            bool algumSeguidorFoiAdicionado;
            do
            {
                Aluno maiorLider = null;
                int maiorQtdeSeguidores = 0;
                int maiorTurno = (int)Turno.Todos;
                foreach (Aluno lider in lideres)
                {
                    // Primeiro faz uma tentativa com os turnos reais dos líderes, depois com qualquer turno
                    int turnoDoLider = lider.TurnoAlocado;
                    int qtdeSeguidores = 0;
                    // Deixa o caso do "Todos" para o loop abaixo, priorizando algum turno real
                    if (turnoDoLider > (int)Turno.Todos)
                    {
                        foreach (Aluno seguidor in seguidores)
                        {
                            // Se a distância até a escola do irmão exceder o limite, ignora
                            if (seguidor.DistanciaAteUnidade(lider.UnidadeAlocada) <= LIMITE_PARA_ALOCACAO_DE_IRMAOS &&
                                lider.UnidadeAlocada.TentarAlocarAlunoDefinicaoInscricao(seguidor, true, rodadas, out turnoAlocado, Motivo.ManterJuntoComIrmaos, turnoDoLider))
                                qtdeSeguidores++;
                        }
                        if (maiorQtdeSeguidores < qtdeSeguidores)
                        {
                            maiorQtdeSeguidores = qtdeSeguidores;
                            maiorLider = lider;
                            maiorTurno = turnoDoLider;
                        }
                    }

                    for (int i = 0; i < TURNOS_PARA_TENTAR_ALOCAR.Length; i++)
                    {
                        int turno = TURNOS_PARA_TENTAR_ALOCAR[i];
                        qtdeSeguidores = 0;
                        foreach (Aluno seguidor in seguidores)
                        {
                            // Se a distância até a escola do irmão exceder o limite, ignora
                            if (seguidor.DistanciaAteUnidade(lider.UnidadeAlocada) <= LIMITE_PARA_ALOCACAO_DE_IRMAOS &&
                                lider.UnidadeAlocada.TentarAlocarAlunoDefinicaoInscricao(seguidor, true, rodadas, out turnoAlocado, Motivo.ManterJuntoComIrmaos, turno))
                                qtdeSeguidores++;
                        }
                        if (maiorQtdeSeguidores < qtdeSeguidores)
                        {
                            maiorQtdeSeguidores = qtdeSeguidores;
                            maiorLider = lider;
                            maiorTurno = turno;
                        }
                    }
                }
                algumSeguidorFoiAdicionado = false;
                if (maiorLider != null)
                {
                    for (int i = seguidores.Count - 1; i >= 0; i--)
                    {
                        // Primeiro tenta alocar de verdade no turno em questão e se der errado,
                        // tenta alocar em qualquer turno (É preciso fazer isso, pois às vezes o
                        // líder estava em um turno diferente do turno dos seguidores)
                        if (maiorLider.UnidadeAlocada.TentarAlocarAlunoDefinicaoInscricao(seguidores[i], false, rodadas, out turnoAlocado, maiorLider.Continuidade ? Motivo.ManterJuntoComIrmaoDeContinuidade : Motivo.ManterJuntoComIrmaos, maiorTurno) ||
                            maiorLider.UnidadeAlocada.TentarAlocarAlunoDefinicaoInscricao(seguidores[i], false, rodadas, out turnoAlocado, maiorLider.Continuidade ? Motivo.ManterJuntoComIrmaoDeContinuidade : Motivo.ManterJuntoComIrmaos))
                        {
                            seguidores.RemoveAt(i);
                            algumSeguidorFoiAdicionado = true;
                        }
                    }
                }
            } while (algumSeguidorFoiAdicionado);

            return JaAlocado;
        }

        private bool TentativaDeAlocacaoSecundariaDeIrmaos(bool rodadas, List<Aluno> seguidores, List<Unidade> unidadesTemp, List<int> turnosTemp, bool assumirApenasPrimeiroSeguidorComoLiderDeficiente)
        {
            int turnoAlocado;

            // Agora que já foi tentado alocar os seguidores em algum líder, se
            // sobraram seguidores vai tentando alocar os seguidores entre si, de
            // forma a tentar manter o maior número possível de seguidores juntos
            // (Testa todas as combinações possíveis, utilizando cada vez um seguidor
            // como se fosse o líder)
            bool algumSeguidorFoiAdicionado;
            do
            {
                Aluno maiorLider = null;
                Unidade maiorUnidade = null;
                int maiorQtdeSeguidores = 0;
                int maiorTurno = (int)Turno.Todos;
                foreach (Aluno lider in seguidores)
                {
                    // Primeiro cria uma lista apenas com as unidades capazes de atender esse
                    // possível líder (em ordem de preferência/distância)
                    unidadesTemp.Clear();
                    turnosTemp.Clear();
                    // Aluno de Definição tem preferência em continuar na mesma escola
                    // que ele está, caso essa escola atenda o tipo de ensino desejado
                    // (independentemente da distância)
                    if ((lider.Definicao || lider.InscricaoDeslocamentoSemEndereco) && lider.UnidadeDeOrigem != null && lider.UnidadeDeOrigem.TentarAlocarAlunoDefinicaoInscricao(lider, true, rodadas, out turnoAlocado, lider.Definicao ? Motivo.DefinicaoContinuidade : Motivo.InscricaoDeslocamentoSemEndereco))
                    {
                        unidadesTemp.Add(lider.UnidadeDeOrigem);
                        turnosTemp.Add(turnoAlocado);
                    }
                    foreach (UnidadeCompatibilizada unidade in lider.UnidadesCompatibilizadas)
                    {
                        if (assumirApenasPrimeiroSeguidorComoLiderDeficiente && !unidade.Unidade.Acessivel)
                            continue;
                        if (lider.DistanciaAteUnidade(unidade.Unidade) <= (unidade.DistanciaAPe ? LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO_APE : LIMITE_PARA_ALOCACAO_DEFINICAO_INSCRICAO) &&
                            unidade.Unidade.TentarAlocarAlunoDefinicaoInscricao(lider, true, rodadas, out turnoAlocado, lider.MotivoBasicoCasoHajaAlocacao))
                        {
                            unidadesTemp.Add(unidade.Unidade);
                            turnosTemp.Add(turnoAlocado);
                        }
                    }
                    // Agora verifica quantos seguidores cabem nas unidades que atenderiam esse
                    // possível líder
                    for (int u = 0; u < unidadesTemp.Count; u++)
                    {
                        // Primeiro faz uma tentativa com os turnos reais dos líderes, depois com qualquer turno
                        Unidade unidade = unidadesTemp[u];
                        int turnoDoLider = turnosTemp[u];
                        int qtdeSeguidores = 0;
                        // Deixa o caso do "Todos" para o loop abaixo, priorizando algum turno real
                        if (turnoDoLider > (int)Turno.Todos)
                        {
                            foreach (Aluno seguidor in seguidores)
                            {
                                if (lider == seguidor) // Não vale a combinação de um aluno consigo próprio
                                    continue;
                                // Se a distância até a escola do irmão exceder o limite, ignora
                                if (seguidor.DistanciaAteUnidade(unidade) <= LIMITE_PARA_ALOCACAO_DE_IRMAOS &&
                                    unidade.TentarAlocarAlunoDefinicaoInscricao(seguidor, true, rodadas, out turnoAlocado, Motivo.ManterJuntoComIrmaos, turnoDoLider))
                                    qtdeSeguidores++;
                            }
                            if (maiorQtdeSeguidores < qtdeSeguidores)
                            {
                                maiorQtdeSeguidores = qtdeSeguidores;
                                maiorUnidade = unidade;
                                maiorLider = lider;
                                maiorTurno = turnoDoLider;
                            }
                        }

                        // Tenta alocar os seguidores (incluindo o próprio líder) na unidade selecionada
                        for (int i = 0; i < TURNOS_PARA_TENTAR_ALOCAR.Length; i++)
                        {
                            int turno = TURNOS_PARA_TENTAR_ALOCAR[i];
                            qtdeSeguidores = 0;
                            foreach (Aluno seguidor in seguidores)
                            {
                                if (lider == seguidor) // Não vale a combinação de um aluno consigo próprio
                                    continue;
                                // Se a distância até a escola do irmão exceder o limite, ignora
                                if (seguidor.DistanciaAteUnidade(unidade) <= LIMITE_PARA_ALOCACAO_DE_IRMAOS &&
                                    unidade.TentarAlocarAlunoDefinicaoInscricao(seguidor, true, rodadas, out turnoAlocado, Motivo.ManterJuntoComIrmaos, turno))
                                    qtdeSeguidores++;
                            }
                            if (maiorQtdeSeguidores < qtdeSeguidores)
                            {
                                maiorQtdeSeguidores = qtdeSeguidores;
                                maiorUnidade = unidade;
                                maiorLider = lider;
                                maiorTurno = turno;
                            }
                        }
                    }

                    if (assumirApenasPrimeiroSeguidorComoLiderDeficiente)
                        break;
                }
                // Tenta alocar os seguidores (incluindo o próprio líder) na unidade selecionada
                algumSeguidorFoiAdicionado = false;
                if (maiorUnidade != null)
                {
                    for (int i = seguidores.Count - 1; i >= 0; i--)
                    {
                        // Primeiro tenta alocar de verdade no turno em questão e se der errado,
                        // tenta alocar em qualquer turno (É preciso fazer isso, pois às vezes o
                        // líder estava em um turno diferente do turno dos seguidores)
                        if (maiorUnidade.TentarAlocarAlunoDefinicaoInscricao(seguidores[i], false, rodadas, out turnoAlocado, Motivo.ManterJuntoComIrmaos, maiorTurno) ||
                            maiorUnidade.TentarAlocarAlunoDefinicaoInscricao(seguidores[i], false, rodadas, out turnoAlocado, Motivo.ManterJuntoComIrmaos))
                        {
                            seguidores.RemoveAt(i);
                            algumSeguidorFoiAdicionado = true;
                        }
                    }
                }
            } while (algumSeguidorFoiAdicionado);

            return JaAlocado;
        }

        public bool TentarSeAlocarJuntoComIrmaosEmAlgumaUnidadeCompatibilizada(bool rodadas, List<Aluno> lideres, List<Aluno> seguidores, List<Unidade> unidadesTemp, List<int> turnosTemp)
        {
            lideres.Clear();
            seguidores.Clear();

            // No caso de deficientes, primeiro tenta alocar usando a regra de deficientes,
            // se não for possível, vai na regra de irmãos regulares, mesmo
            if (Deficiente)
            {

                // Como a continuidade roda primeiro, se algum irmão dele já estiver matriculado,
                // é de continuidade (tenta apenas alocar este irmão deficiente com o outro irmão
                // de continuidade, que não pode ser movido)

                foreach (Aluno aluno in Irmaos.Values)
                {
                    if (aluno.JaAlocado)
                        lideres.Add(aluno);
                }

                seguidores.Add(this);

                if (TentativaDeAlocacaoPadraoDeIrmaos(rodadas, lideres, seguidores, unidadesTemp))
                    return true;

                // Como não foi possível alocar esse aluno deficiente junto de seu irmão
                // já matriculado, tenta alocar o aluno deficiente, e depois seus demais
                // irmãos ainda não alocados na mesma escola desse aluno
                foreach (Aluno aluno in Irmaos.Values)
                {
                    if (!aluno.JaAlocado)
                        seguidores.Add(aluno);
                }

                if (TentativaDeAlocacaoSecundariaDeIrmaos(rodadas, seguidores, unidadesTemp, turnosTemp, true))
                    return true;

                // Se ainda assim não deu, tenta prosseguir como um irmão regular
            }

            lideres.Clear();
            seguidores.Clear();

            // A ideia será: vamos tentar alocar os "seguidores" (irmãos ainda não alocados)
            // nas mesmas unidades dos "líderes" (irmãos já alocados)
            foreach (Aluno aluno in Irmaos.Values)
            {
                if (aluno.JaAlocado)
                    lideres.Add(aluno);
            }

            seguidores.Add(this);
            foreach (Aluno aluno in Irmaos.Values)
            {
                if (!aluno.JaAlocado)
                    seguidores.Add(aluno);
            }

            if (TentativaDeAlocacaoPadraoDeIrmaos(rodadas, lideres, seguidores, unidadesTemp))
                return true;

            // Agora que já foi tentado alocar os seguidores em algum líder, se
            // sobraram seguidores vai tentando alocar os seguidores entre si, de
            // forma a tentar manter o maior número possível de seguidores juntos
            // (Testa todas as combinações possíveis, utilizando cada vez um seguidor
            // como se fosse o líder)
            if (TentativaDeAlocacaoSecundariaDeIrmaos(rodadas, seguidores, unidadesTemp, turnosTemp, false))
                return true;

            // Aqui, se o aluno atual ainda não foi alocado, aloca ele como um aluno
            // regular e ignora os outros seguidores que possam ter sobrado (eles serão
            // alocados como alunos regulares em algum ponto do futuro)
            return TentarSeAlocarEmAlgumaUnidadeCompatibilizada(rodadas);
        }

        public bool PrecisaDeTransporte
        {
            get
            {
                if (TurmaAlocada == null)
                    return false;
                UnidadeCompatibilizada unidade;
                if (!UnidadesCompatibilizadas.ValorDaChave(TurmaAlocada.Unidade.CodigoUnidadeCorrigido, out unidade))
                    return false;
                return (unidade.TemBarreira || unidade.Distancia > 2000.0);
            }
        }

        #endregion

        #region Irmãos

        public static int AssociarIrmaos(Dictionary<int, Aluno> alunos, int aluno1, int aluno2, bool gemeo)
        {
            Aluno irmao1, irmao2;
            if (!alunos.TryGetValue(aluno1, out irmao1))
            {
                // Nada feito...
                return (alunos.TryGetValue(aluno2, out irmao2) ? -1 : 0);
            }
            else if (!alunos.TryGetValue(aluno2, out irmao2))
            {
                // Nada feito...
                return -2;
            }

            //se o um dos irmãos for inscrição de intenção de transferencia eles não devem ser associados
            if (irmao1.InscricaoDeslocamentoSemEndereco || irmao2.InscricaoDeslocamentoSemEndereco) return 1;

            // Faz a associação cruzada
            irmao1.Irmaos[aluno2] = irmao2;
            irmao2.Irmaos[aluno1] = irmao1;
            if (gemeo)
            {
                irmao1.Gemeo = true;
                irmao2.Gemeo = true;
            }
            return 1;
        }

        public void LimparIrmaos()
        {
            Irmaos.Clear();
        }

        public void AdicionarIrmaoFake(Aluno irmao, bool gemeo)
        {
            irmao.Irmaos[Codigo] = this;
            Irmaos[irmao.Codigo] = irmao;
            if (gemeo)
            {
                Gemeo = gemeo;
                irmao.Gemeo = gemeo;
            }
        }

        public int QuantidadeIrmaosParaSerializacao
        {
            get
            {
                // Para economizar espaço, serializa apenas os irmãos do aluno com menor Codigo
                int total = 0;
                foreach (Aluno irmao in Irmaos.Values)
                {
                    if (Codigo < irmao.Codigo)
                        total++;
                }
                return total;
            }
        }

        public bool PossuiIrmao
        {
            get { return Irmaos.Values.Count > 0; }
        }

        public void ColetarParesDeIrmaos(List<Irmao> paresDeIrmaos)
        {
            // Para evitar duplicidades, coleta apenas os irmãos do aluno com menor Codigo
            foreach (Aluno irmao in Irmaos.Values)
            {
                if (Codigo < irmao.Codigo || irmao.AlunoFakeApenasParaSerIrmao)
                    paresDeIrmaos.Add(new Irmao(this, irmao));
            }
        }

        public int ContarIrmaosDistantes(Dictionary<int, Unidade> unidades, StringBuilder sb)
        {
            // Para evitar duplicidades, conta apenas os irmãos do aluno com menor Codigo
            int total = 0;
            foreach (Aluno irmao in Irmaos.Values)
            {
                if (Codigo < irmao.Codigo)
                {
                    if (Math.Abs(this.Coordenada.Latitude - irmao.Coordenada.Latitude) > 0.01 ||
                        Math.Abs(this.Coordenada.Longitude - irmao.Coordenada.Longitude) > 0.01)
                    {

                        if (UnidadeDeOrigem != null)
                        {
                            sb.Append(UnidadeDeOrigem.NomeDiretoria);
                            sb.Append('\t');
                            sb.Append(UnidadeDeOrigem.NomeMunicipio);
                            sb.Append('\t');
                            sb.Append(UnidadeDeOrigem.CodigoEscola);
                            sb.Append('\t');
                            sb.Append(UnidadeDeOrigem.NomeEscola);
                            sb.Append('\t');
                        }
                        else
                        {
                            sb.Append("");
                            sb.Append('\t');
                            sb.Append("");
                            sb.Append('\t');
                            sb.Append("");
                            sb.Append('\t');
                            sb.Append("");
                            sb.Append('\t');
                        }

                        sb.Append(Codigo);
                        sb.Append('\t');
                        sb.Append(RA);
                        sb.Append('\t');
                        sb.Append(Nome);
                        sb.Append('\t');
                        sb.Append(Coordenada.Latitude);
                        sb.Append('\t');
                        sb.Append(Coordenada.Longitude);
                        sb.Append('\t');

                        if (irmao.UnidadeDeOrigem != null)
                        {
                            sb.Append(irmao.UnidadeDeOrigem.NomeDiretoria);
                            sb.Append('\t');
                            sb.Append(irmao.UnidadeDeOrigem.NomeMunicipio);
                            sb.Append('\t');
                            sb.Append(irmao.UnidadeDeOrigem.CodigoEscola);
                            sb.Append('\t');
                            sb.Append(irmao.UnidadeDeOrigem.NomeEscola);
                            sb.Append('\t');
                        }
                        else
                        {
                            sb.Append("");
                            sb.Append('\t');
                            sb.Append("");
                            sb.Append('\t');
                            sb.Append("");
                            sb.Append('\t');
                            sb.Append("");
                            sb.Append('\t');
                        }

                        sb.Append(irmao.Codigo);
                        sb.Append('\t');
                        sb.Append(irmao.RA);
                        sb.Append('\t');
                        sb.Append(irmao.Nome);
                        sb.Append('\t');
                        sb.Append(irmao.Coordenada.Latitude);
                        sb.Append('\t');
                        sb.Append(irmao.Coordenada.Longitude);
                        sb.AppendLine();

                        total++;
                    }
                }
            }
            return total;
        }

        public void SerializarIrmaos(System.IO.BinaryWriter writer)
        {
            // Para economizar espaço, serializa apenas os irmãos do aluno com menor Codigo
            foreach (Aluno irmao in Irmaos.Values)
            {
                if (Codigo < irmao.Codigo)
                {
                    writer.Write(Codigo);
                    writer.Write(irmao.Codigo);
                    writer.Write(Gemeo);
                }
            }
        }

        public static void DeserializarIrmaos(System.IO.BinaryReader reader, Dictionary<int, Aluno> alunos)
        {
            int total = reader.ReadInt32();

            for (int i = 0; i < total; i++)
                AssociarIrmaos(alunos, reader.ReadInt32(), reader.ReadInt32(), reader.ReadBoolean());
        }

        #endregion

        #region Relatório
        public void Relatorio(StringBuilder builder)
        {
            builder.Append(Nome);
            builder.Append('\t');
            builder.Append(RA);
            builder.Append('\t');
            builder.Append(Codigo);
            builder.Append('\t');
            builder.Append(CodigoFicha == 0 ? "" : CodigoFicha.ToString());
            builder.Append('\t');
            builder.Append(FaseFicha.ToString());
            builder.Append('\t');
            builder.Append(Continuidade ? "CONTINUIDADE" : (Definicao ? "DEFINICAO" : "INSCRICAO"));
            builder.Append('\t');
            builder.Append(Deficiente ? "SIM" : "NÃO");
            builder.Append('\t');
            builder.Append(InteresseNoturno ? "SIM" : "NÃO");
            builder.Append('\t');
            builder.Append(InteresseIntegral ? "SIM" : "NÃO");
            builder.Append('\t');
            builder.Append(TipoEnsinoDesejado);
            builder.Append('\t');
            builder.Append(SerieDesejada);
            builder.Append('\t');
            builder.Append(TurnoDesejado);
            builder.Append('\t');
            if (UnidadeDeOrigem == null)
            {
                //builder.Append(UnidadeDeOrigem.NomeDiretoria);
                builder.Append('\t');
                //builder.Append(UnidadeDeOrigem.NomeMunicipio);
                builder.Append('\t');
                //builder.Append(UnidadeDeOrigem.CodigoRedeEnsino);
                builder.Append('\t');
                builder.Append(EscolaDeOrigem);
                builder.Append('\t');
                //builder.Append(UnidadeDeOrigem.NomeEscola);
                builder.Append('\t');
                builder.Append(UnidadeDeOrigemCorrigido > (int.MinValue + 1) ? UnidadeDeOrigemCorrigido : 1);
                builder.Append('\t');
            }
            else
            {
                builder.Append(UnidadeDeOrigem.NomeDiretoria);
                builder.Append('\t');
                builder.Append(UnidadeDeOrigem.NomeMunicipio);
                builder.Append('\t');
                builder.Append(UnidadeDeOrigem.CodigoRedeEnsino);
                builder.Append('\t');
                builder.Append(UnidadeDeOrigem.CodigoEscola);
                builder.Append('\t');
                builder.Append(UnidadeDeOrigem.NomeEscola);
                builder.Append('\t');
                builder.Append(UnidadeDeOrigem.CodigoUnidadeNoBanco);
                builder.Append('\t');
            }
            if (Continuidade)
            {
                builder.Append(TurmaDeOrigem);
                builder.Append('\t');
                builder.Append(TurnoDeOrigem);
                builder.Append('\t');
                builder.Append(DuracaoDeOrigem);
                builder.Append('\t');
                builder.Append(IdTurmaDeOrigem);
                builder.Append('\t');
                builder.Append(SalaDeOrigem);
                builder.Append('\t');
                //builder.Append(UnidadesCompatibilizadas.Count == 0 ? "SIM" : "NÃO");
                builder.Append('\t');
            }
            else
            {
                //builder.Append(TurmaDeOrigem);
                builder.Append('\t');
                //builder.Append(TurnoDeOrigem);
                builder.Append('\t');
                //builder.Append(DuracaoDeOrigem);
                builder.Append('\t');
                //builder.Append(IdTurmaDeOrigem);
                builder.Append('\t');
                //builder.Append(SalaDeOrigem);
                builder.Append('\t');
                builder.Append(UnidadesCompatibilizadas.Quantidade == 0 ? "SIM" : "NÃO");
                builder.Append('\t');
            }
            if (UnidadeAlocada == null)
            {
                //builder.Append((UnidadeAlocada.CodigoUnidadeCorrigido != UnidadeDeOrigemCorrigido) ? "SIM" : "NÃO");
                builder.Append('\t');
                //builder.Append(UnidadeAnteriorCorrigido_089 > (int.MinValue + 1) ? UnidadeAnteriorCorrigido_089 : 1);
                builder.Append('\t');
                //builder.Append(UnidadeAlocada.NomeDiretoria);
                builder.Append('\t');
                //builder.Append(UnidadeAlocada.NomeMunicipio);
                builder.Append('\t');
                //builder.Append(UnidadeAlocada.CodigoRedeEnsino);
                builder.Append('\t');
                //builder.Append(UnidadeAlocada.CodigoEscola);
                builder.Append('\t');
                //builder.Append(UnidadeAlocada.NomeEscola);
                builder.Append('\t');
                //builder.Append(UnidadeAlocada.CodigoUnidadeNoBanco);
                builder.Append('\t');
                //builder.Append(UnidadeAlocada.Acessivel ? "SIM" : "NÃO");
                builder.Append('\t');
            }
            else
            {
                builder.Append((UnidadeAlocada.CodigoUnidadeCorrigido != UnidadeDeOrigemCorrigido) ? "SIM" : "NÃO");
                builder.Append('\t');
                builder.Append(UnidadeAnteriorCorrigido_089 > (int.MinValue + 1) ? UnidadeAnteriorCorrigido_089 : 1);
                builder.Append('\t');
                builder.Append(UnidadeAlocada.NomeDiretoria);
                builder.Append('\t');
                builder.Append(UnidadeAlocada.NomeMunicipio);
                builder.Append('\t');
                builder.Append(UnidadeAlocada.CodigoRedeEnsino);
                builder.Append('\t');
                builder.Append(UnidadeAlocada.CodigoEscola);
                builder.Append('\t');
                builder.Append(UnidadeAlocada.NomeEscola);
                builder.Append('\t');
                builder.Append(UnidadeAlocada.CodigoUnidadeNoBanco);
                builder.Append('\t');
                builder.Append(UnidadeAlocada.Acessivel ? "SIM" : "NÃO");
                builder.Append('\t');
            }
            if (TurmaAlocada == null)
            {
                //builder.Append(TurmaAlocada.NumeroClasse);
                builder.Append('\t');
                //builder.Append(TurmaAlocada.Descricao);
                builder.Append('\t');
                //builder.Append(TurmaAlocada.IdTurma);
                builder.Append('\t');
                //builder.Append(TurmaAlocada.Sala);
                builder.Append('\t');
            }
            else
            {
                builder.Append(TurmaAlocada.NumeroClasse);
                builder.Append('\t');
                builder.Append(TurmaAlocada.Descricao);
                builder.Append('\t');
                builder.Append(TurmaAlocada.IdTurma);
                builder.Append('\t');
                builder.Append(TurmaAlocada.Sala);
                builder.Append('\t');
            }
            builder.Append(MotivoStr);
            builder.AppendLine();
        }
        #endregion

        #region SQL
        private static string N(string s)
        {
            return (s == null ? "" : s.Trim().Replace('\"', ' ').Replace('\'', ' ').Replace('\r', ' ').Replace('\n', ' '));
        }

        public void ExportarDados(DataTable tbl, int idRodada)
        {
            DataRow row;

            bool alocado;
            int irmao = 0;

            Unidade unidade;
            if (UnidadeAlocada != null)
            {
                unidade = UnidadeAlocada;
                alocado = true;
            }
            else if (UnidadeDeOrigem != null)
            {
                unidade = UnidadeDeOrigem;
                alocado = false;
            }
            else
            {
                row = tbl.NewRow();
                row["CD_DIRETORIA"] = 0;
                row["NM_DIRETORIA"] = "";
                row["CD_DNE"] = 0;
                row["NM_MUNICIPIO"] = "";
                row["CD_REDE_ENSINO"] = 0;
                row["DS_REDE_ENSINO"] = "";
                row["CD_ESCOLA"] = 0;
                row["NM_COMPLETO_ESCOLA"] = "";
                row["CD_UNIDADE"] = 0;
                row["CD_TIPO_ENSINO"] = TipoEnsinoDesejado;
                row["NM_TIPO_ENSINO"] = Unidade.Turma.DescricaoTipoEnsinoGeral(TipoEnsinoDesejado);
                row["NR_SERIE"] = SerieDesejada;
                row["CD_TURMA"] = 0;
                row["NR_CLASSE"] = 0;
                row["CD_ALUNO"] = Codigo;
                row["NM_ALUNO"] = Nome;
                row["NR_RA"] = RA;
                row["ID_FICHA"] = CodigoFicha;
                row["ID_FASE"] = FaseFicha;
                row["DEFICIENTE"] = Deficiente;
                foreach (int i in Irmaos.Keys)
                {
                    irmao = i;
                    break;
                }
                row["CD_IRMAO"] = irmao;
                row["GEMEO"] = Gemeo;
                row["ALOCADO"] = false;
                row["MOTIVO"] = MotivoStr;
                row["DT_ALOC"] = DBNull.Value;
                row["CD_ESCOL_ALOC"] = DBNull.Value;
                row["NUM_CLASSE_ALOC"] = DBNull.Value;
                row["CD_ESCOL_COMP_DEF"] = DBNull.Value;
                row["CD_UNID_COMP_DEF"] = DBNull.Value;
                row["FL_FASE_COMP"] = DBNull.Value;
                row["TP_COMP_DEF"] = DBNull.Value;
                row["FL_COMP_DEF"] = DBNull.Value;
                row["DT_COMP_DEF"] = DBNull.Value;
                row["HR_COMP_DEF"] = DBNull.Value;
                row["ID_SIT_COMP_DEF"] = DBNull.Value;
                row["SITUACAO_COMP_DEF"] = DBNull.Value;
                row["ID_RODADA"] = idRodada;
                row["CD_MATRICULA_ALUNO"] = 0L; // CodigoMatriculaSendoCriada deveria ser 0 aqui!
                row["CD_MATRICULA_ALUNO_BKP_FI"] = DBNull.Value;
                row["USOU_LATLNG_DA_ESCOLA"] = false;
                row["LAT_COMP_DEF"] = CoordenadaValida ? Coordenada.Latitude : 0;
                row["LNG_COMP_DEF"] = CoordenadaValida ? Coordenada.Longitude : 0;
                row["DISTANCIA_COMP_DEF"] = DBNull.Value;
                row["CD_ENDERECO_ALUNO"] = CodigoEndereco;
                tbl.Rows.Add(row);
                return;
            }

            row = tbl.NewRow();
            row["CD_DIRETORIA"] = unidade.CodigoDiretoriaEstadual;
            row["NM_DIRETORIA"] = N(unidade.NomeDiretoria);
            row["CD_DNE"] = unidade.CodigoMunicipio;
            row["NM_MUNICIPIO"] = N(unidade.NomeMunicipio);
            row["CD_REDE_ENSINO"] = unidade.CodigoRedeEnsino;
            row["DS_REDE_ENSINO"] = N(unidade.DescricaoRedeEnsino);
            row["CD_ESCOLA"] = unidade.CodigoEscola;
            row["NM_COMPLETO_ESCOLA"] = N(unidade.NomeEscola);
            row["CD_UNIDADE"] = unidade.CodigoUnidadeNoBanco;
            row["CD_TIPO_ENSINO"] = TipoEnsinoDesejado;
            row["NM_TIPO_ENSINO"] = Unidade.Turma.DescricaoTipoEnsinoGeral(TipoEnsinoDesejado);
            row["NR_SERIE"] = SerieDesejada;
            row["CD_TURMA"] = ((!alocado || TurmaAlocada == null) ? 0 : (unidade.ConsiderarApenasVagasDaUnidade ? 0 : TurmaAlocada.CodigoTurma));
            row["NR_CLASSE"] = ((!alocado || TurmaAlocada == null) ? 0 : (unidade.ConsiderarApenasVagasDaUnidade ? 0 : TurmaAlocada.NumeroClasse));
            row["CD_ALUNO"] = Codigo;
            row["NM_ALUNO"] = Nome;
            row["NR_RA"] = RA;
            row["ID_FICHA"] = CodigoFicha;
            row["ID_FASE"] = FaseFicha;
            row["DEFICIENTE"] = Deficiente;
            foreach (int i in Irmaos.Keys)
            {
                irmao = i;
                break;
            }
            row["CD_IRMAO"] = irmao;
            row["GEMEO"] = Gemeo;
            row["ALOCADO"] = alocado;
            row["MOTIVO"] = MotivoStr;
            row["ID_RODADA"] = idRodada;
            row["CD_MATRICULA_ALUNO"] = CodigoMatriculaSendoCriada;
            row["USOU_LATLNG_DA_ESCOLA"] = (ForcarUtilizacaoDasCoordenadasDaEscolaDeOrigemPorFaltaDeEscolas ||
                (UnidadeDeOrigem != null && DistanciaGeodesica(UnidadeDeOrigem) <= 1.0));

            //grava dados da GEO
            row["LAT_COMP_DEF"] = CoordenadaValida ? Coordenada.Latitude : 0;
            row["LNG_COMP_DEF"] = CoordenadaValida ? Coordenada.Longitude : 0;
            if (alocado)
            {
                if (unidade.CoordenadaValida && CoordenadaValida)
                {
                    double distancia = Coordenada.DistanciaGeodesica(unidade.Coordenada);
                    row["DISTANCIA_COMP_DEF"] = distancia <= 0 ? 0 : distancia;
                }
                else
                {
                    row["DISTANCIA_COMP_DEF"] = DBNull.Value;
                }
            }
            else
            {
                row["DISTANCIA_COMP_DEF"] = DBNull.Value;
            }
            row["CD_ENDERECO_ALUNO"] = CodigoEndereco;
            tbl.Rows.Add(row);
        }

        public void ExportarDadosDePara(DataTable tbl)
        {
            DataRow row;

            row = tbl.NewRow();
            row["CD_ALUNO"] = Codigo;
            row["ID_FICHA"] = CodigoFicha;
            row["CD_DIRETORIA_DE"] = UnidadeDeOrigem.CodigoDiretoriaEstadual;
            row["NM_DIRETORIA_DE"] = N(UnidadeDeOrigem.NomeDiretoria);
            row["CD_DNE_DE"] = UnidadeDeOrigem.CodigoMunicipio;
            row["NM_MUNICIPIO_DE"] = N(UnidadeDeOrigem.NomeMunicipio);
            row["CD_REDE_ENSINO_DE"] = UnidadeDeOrigem.CodigoRedeEnsino;
            row["DS_REDE_ENSINO_DE"] = N(UnidadeDeOrigem.DescricaoRedeEnsino);
            row["CD_ESCOLA_DE"] = UnidadeDeOrigem.CodigoEscola;
            row["NM_COMPLETO_ESCOLA_DE"] = N(UnidadeDeOrigem.NomeEscola);
            row["CD_UNIDADE_DE"] = UnidadeDeOrigem.CodigoUnidadeNoBanco;
            tbl.Rows.Add(row);
        }

        public void ExportarDadosMunicipio(DataTable tbl, DateTime dataCompat, string dataCompatStrYMA, string dataCompatStrHora)
        {
            DataRow row;

            row = tbl.NewRow();
            row["CD_ALUNO"] = Codigo;
            row["ID_FICHA"] = CodigoFicha;
            row["CD_ESCOLA_INSCRICAO"] = (UnidadeDeOrigem == null ? DBNull.Value : (object)UnidadeDeOrigem.CodigoEscola);
            row["NR_RA"] = NormalizarRA(RA);
            row["NR_DIG_RA"] = DBNull.Value; // Virá do UPDATE
            row["SG_UF_RA"] = DBNull.Value; // Virá do UPDATE
            row["ID_GRAU"] = TipoEnsinoDesejado;
            row["ID_SERIE"] = SerieDesejada;
            row["FL_FASE"] = FaseFicha;
            row["DEFICIENTE"] = Deficiente;
            row["DS_LATITUDE"] = (CoordenadaValida ? (object)Coordenada.Latitude : DBNull.Value);
            row["DS_LONGITUDE"] = (CoordenadaValida ? (object)Coordenada.Longitude : DBNull.Value);
            row["EN_RUA"] = DBNull.Value; // Virá do UPDATE
            row["EN_NR_EN"] = DBNull.Value; // Virá do UPDATE
            row["NM_BAIRRO"] = DBNull.Value; // Virá do UPDATE
            row["NR_CEP"] = DBNull.Value; // Virá do UPDATE
            row["TIPO_ENDERECO_COMP_DEF"] = true; // Virá do UPDATE
            row["REDE_ENSINO"] = (UnidadeAlocada == null ? DBNull.Value : (object)UnidadeAlocada.CodigoRedeEnsino);
            row["CD_ESCOLA_COMP_DEF"] = (UnidadeAlocada == null ? DBNull.Value : (object)UnidadeAlocada.CodigoEscola);
            row["CD_UNIDADE_COMP_DEF"] = (UnidadeAlocada == null ? DBNull.Value : (object)UnidadeAlocada.CodigoUnidadeNoBanco);
            row["DATA_INSCRICAO"] = DBNull.Value; // Virá do UPDATE
            row["DATA_NASCTO"] = DBNull.Value; // Virá do UPDATE
            row["NOME_ALUNO"] = Nome;
            row["NOME_MAE"] = DBNull.Value; // Virá do UPDATE
            row["NOME_PAI"] = DBNull.Value; // Virá do UPDATE
            row["SEXO"] = DBNull.Value; // Virá do UPDATE
            row["IRMAO"] = (Irmaos.Count > 0);
            row["GEMEO"] = Gemeo;
            row["ESCOLA_ALOCACAO"] = ((UnidadeAlocada == null || UnidadeAlocada.CodigoRedeEnsino != 1) ? DBNull.Value : (object)UnidadeAlocada.CodigoEscola);
            row["DATA_COMP_DEF"] = dataCompatStrYMA;
            row["HORA_COMP_DEF"] = dataCompatStrHora;
            row["FLAG_COMP_DEF"] = JaAlocado;
            row["SITUACAO_COMP_DEF"] = MotivoStr;
            row["INTERESSE_INTEGRAL"] = InteresseIntegral;
            tbl.Rows.Add(row);
        }

        public void ExportarDadosMunicipio(StringBuilder builder)
        {
            if (UnidadeDeOrigem == null)
            {
                //builder.Append(UnidadeAlocada.CodigoDiretoriaEstadual);
                builder.Append('\t');
                //builder.Append(N(UnidadeAlocada.NomeDiretoria));
                builder.Append('\t');
                //builder.Append(UnidadeAlocada.CodigoMunicipio);
                builder.Append('\t');
                //builder.Append(N(UnidadeAlocada.NomeMunicipio));
                builder.Append('\t');
                //builder.Append(UnidadeAlocada.CodigoRedeEnsino);
                builder.Append('\t');
                //builder.Append(N(UnidadeAlocada.DescricaoRedeEnsino));
                builder.Append('\t');
                //builder.Append(UnidadeAlocada.CodigoEscola);
                builder.Append('\t');
                //builder.Append(N(UnidadeAlocada.NomeEscola));
                builder.Append('\t');
                //builder.Append(UnidadeAlocada.CodigoUnidadeNoBanco);
                builder.Append('\t');
            }
            else
            {
                builder.Append(UnidadeDeOrigem.CodigoDiretoriaEstadual);
                builder.Append('\t');
                builder.Append(N(UnidadeDeOrigem.NomeDiretoria));
                builder.Append('\t');
                builder.Append(UnidadeDeOrigem.CodigoMunicipio);
                builder.Append('\t');
                builder.Append(N(UnidadeDeOrigem.NomeMunicipio));
                builder.Append('\t');
                builder.Append(UnidadeDeOrigem.CodigoRedeEnsino);
                builder.Append('\t');
                builder.Append(N(UnidadeDeOrigem.DescricaoRedeEnsino));
                builder.Append('\t');
                builder.Append(UnidadeDeOrigem.CodigoEscola);
                builder.Append('\t');
                builder.Append(N(UnidadeDeOrigem.NomeEscola));
                builder.Append('\t');
                builder.Append(UnidadeDeOrigem.CodigoUnidadeNoBanco);
                builder.Append('\t');
            }
            if (UnidadeAlocada == null)
            {
                builder.Append("NÃO\t");
                //builder.Append(UnidadeAlocada.CodigoDiretoriaEstadual);
                builder.Append('\t');
                //builder.Append(N(UnidadeAlocada.NomeDiretoria));
                builder.Append('\t');
                //builder.Append(UnidadeAlocada.CodigoMunicipio);
                builder.Append('\t');
                //builder.Append(N(UnidadeAlocada.NomeMunicipio));
                builder.Append('\t');
                //builder.Append(UnidadeAlocada.CodigoRedeEnsino);
                builder.Append('\t');
                //builder.Append(N(UnidadeAlocada.DescricaoRedeEnsino));
                builder.Append('\t');
                //builder.Append(UnidadeAlocada.CodigoEscola);
                builder.Append('\t');
                //builder.Append(N(UnidadeAlocada.NomeEscola));
                builder.Append('\t');
                //builder.Append(UnidadeAlocada.CodigoUnidadeNoBanco);
                builder.Append('\t');
            }
            else
            {
                builder.Append("SIM\t");
                builder.Append(UnidadeAlocada.CodigoDiretoriaEstadual);
                builder.Append('\t');
                builder.Append(N(UnidadeAlocada.NomeDiretoria));
                builder.Append('\t');
                builder.Append(UnidadeAlocada.CodigoMunicipio);
                builder.Append('\t');
                builder.Append(N(UnidadeAlocada.NomeMunicipio));
                builder.Append('\t');
                builder.Append(UnidadeAlocada.CodigoRedeEnsino);
                builder.Append('\t');
                builder.Append(N(UnidadeAlocada.DescricaoRedeEnsino));
                builder.Append('\t');
                builder.Append(UnidadeAlocada.CodigoEscola);
                builder.Append('\t');
                builder.Append(N(UnidadeAlocada.NomeEscola));
                builder.Append('\t');
                builder.Append(UnidadeAlocada.CodigoUnidadeNoBanco);
                builder.Append('\t');
            }
            builder.Append(TipoEnsinoDesejado);
            builder.Append('\t');
            builder.Append(Unidade.Turma.DescricaoTipoEnsinoGeral(TipoEnsinoDesejado));
            builder.Append('\t');
            builder.Append(SerieDesejada);
            builder.Append('\t');
            builder.Append(Codigo);
            builder.Append('\t');
            builder.Append(N(Nome));
            builder.Append('\t');
            builder.Append(NormalizarRA(RA));
            builder.Append('\t');
            builder.Append(CodigoFicha);
            builder.Append('\t');
            builder.Append(FaseFicha);
            builder.Append('\t');
            builder.Append(Deficiente ? "SIM" : "NÃO");
            builder.Append('\t');
            int irmao = 0;
            foreach (int i in Irmaos.Keys)
            {
                irmao = i;
                break;
            }
            builder.Append(irmao);
            builder.Append('\t');
            builder.Append(Gemeo ? "SIM" : "NÃO");
            builder.Append('\t');
            builder.Append(MotivoStr);
            builder.AppendLine();
        }

        public void ExportarDados15(DataTable tbl)
        {
            DataRow row;

            foreach (UnidadeCompatibilizada unidade in UnidadesCompatibilizadas)
            {
                row = tbl.NewRow();
                row["CD_ALUNO"] = Codigo;
                row["DS_LAT_ALUNO"] = Coordenada.Latitude;
                row["DS_LNG_ALUNO"] = Coordenada.Longitude;
                row["CD_ESCOLA"] = unidade.Unidade.CodigoEscola;
                row["NM_COMPLETO_ESCOLA"] = unidade.Unidade.NomeEscola;
                row["DS_REDE_ENSINO"] = unidade.Unidade.DescricaoRedeEnsino;
                row["DS_LAT_ESCOLA"] = unidade.Unidade.Coordenada.Latitude;
                row["DS_LNG_ESCOLA"] = unidade.Unidade.Coordenada.Longitude;
                row["DS_DISTANCIA"] = unidade.Distancia;
                row["DS_ROTA"] = unidade.Poligono;
                tbl.Rows.Add(row);
            }
        }

        public void Matricular(DataTable tbl, string anoLetivoStr, DateTime dataHoraCompat, DateTime dataCompat, string dataCompatStrYMA, string dataCompatStrHora5, bool rodadas)
        {
            DataRow row;

            DateTime dataInicioMatricula = dataCompat;
            if (dataInicioMatricula < TurmaAlocada.DataInicio)
                dataInicioMatricula = TurmaAlocada.DataInicio;

            row = tbl.NewRow();
            row["CD_MATRICULA_ALUNO"] = CodigoMatriculaSendoCriada;
            row["CD_ALUNO"] = Codigo;
            row["NR_SERIE"] = TurmaAlocada.Serie;
            row["FL_SITUACAO_ALUNO_CLASSE"] = 0;
            row["CD_TURMA"] = TurmaAlocada.CodigoTurma;
            row["CD_ESCOLA"] = UnidadeAlocada.CodigoEscola;
            row["NR_GRAU"] = TurmaAlocada.TipoEnsino;
            row["NR_ALUNO"] = ++TurmaAlocada.MaiorNrAluno;
            row["DT_INICIO_MATRICULA"] = dataInicioMatricula;
            row["DT_FIM_MATRICULA"] = TurmaAlocada.DataFim;
            row["CD_GRAU_NIVEL"] = 0;
            row["CD_SERIE_NIVEL"] = 0;
            row["DT_ENVIO"] = dataCompatStrYMA;
            row["NR_CLASSE"] = TurmaAlocada.NumeroClasse;
            row["DT_INCL"] = dataHoraCompat;
            row["HR_INCL"] = dataCompatStrHora5;
            row["HR_ENVIO"] = dataCompatStrHora5;
            row["DT_INCL_MATRIC"] = dataHoraCompat;
            row["HR_INCL_MATRIC"] = dataCompatStrHora5;
            row["DT_ANO_LETIVO"] = anoLetivoStr;
            row["LOGIN_INCL"] = "[Compatibilizacao]";
            row["MACHINE_INCL"] = "[Compatibilizacao]";
            row["USER_INCL"] = "[Compatibilizacao]";
            row["FL_COMPAT_MANUAL"] = false;
            int equiv = TipoEnsinoSerie.GerarEquivalente(TurmaAlocada.TipoEnsino, TurmaAlocada.Serie);
            row["CD_TIPO_ENSINO_EQUIVALENTE"] = TipoEnsinoSerie.TipoEnsino(equiv);
            row["NR_SERIE_EQUIVALENTE"] = TipoEnsinoSerie.Serie(equiv);
            row["CD_MATRICULA_ALUNO_ANTERIOR"] = (CodigoMatriculaAnterior_089 <= 0 ? DBNull.Value : (object)CodigoMatriculaAnterior_089);
            row["ID_FICHA_INSCRICAO"] = (CodigoFicha <= 0 ? DBNull.Value : (object)CodigoFicha);
            row["CD_TIPO_EXCECAO"] = CodigoTipoExcecao;
            tbl.Rows.Add(row);
        }
        #endregion
    }
}
