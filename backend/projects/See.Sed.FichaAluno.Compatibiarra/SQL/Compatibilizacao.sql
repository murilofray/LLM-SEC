﻿CREATE TABLE CADALUNOS..TB_COMPAT_MUNICIPIO (
	CD_COMPAT_MUNICIPIO INT NOT NULL IDENTITY PRIMARY KEY,
	ANO_LETIVO VARCHAR(4) NOT NULL,
	CD_DNE INT NOT NULL
)

CREATE NONCLUSTERED INDEX IX_COMPAT_MUNICIPIO_ANO ON CADALUNOS..TB_COMPAT_MUNICIPIO (ANO_LETIVO ASC, CD_DNE ASC)

CREATE TABLE CADALUNOS..TB_Compatibiarra (
	CD_Compatibiarra INT NOT NULL IDENTITY PRIMARY KEY,
	FL_Compatibiarra BIT NOT NULL
)

-- Armazena as 15 escolas mais próximas do aluno, com base no tipo de ensino
CREATE TABLE CADALUNOS..TB_REL_COMPAT_15 (
	ID_REL_COMPAT_15 BIGINT NOT NULL IDENTITY PRIMARY KEY,
	CD_ALUNO INT NOT NULL,
	DS_LAT_ALUNO FLOAT NOT NULL,
	DS_LNG_ALUNO FLOAT NOT NULL,
	CD_ESCOLA INT NOT NULL,
	NM_COMPLETO_ESCOLA VARCHAR(100) NOT NULL,
	DS_REDE_ENSINO VARCHAR(60) NOT NULL,
	DS_LAT_ESCOLA FLOAT NOT NULL,
	DS_LNG_ESCOLA FLOAT NOT NULL,
	DS_DISTANCIA FLOAT NOT NULL,
	DS_ROTA VARCHAR(MAX) NOT NULL,
)

CREATE NONCLUSTERED INDEX IX_REL_COMPAT_15 ON CADALUNOS..TB_REL_COMPAT_15 (CD_ALUNO ASC)

-- Dados do município CBI
CREATE TABLE CADALUNOS..TB_REL_COMPAT_CBI (
	ID_REL_COMPAT_CBI BIGINT NOT NULL IDENTITY PRIMARY KEY,
	CD_ALUNO INT NOT NULL,
	ID_FICHA INT NOT NULL,
	CD_ESCOLA_INSCRICAO INT NULL,
	NR_RA VARCHAR(12) NULL,
	NR_DIG_RA VARCHAR(2) NULL,
	SG_UF_RA VARCHAR(2) NULL,
	ID_GRAU INT NULL,
	ID_SERIE INT NULL,
	FL_FASE INT NULL,
	DEFICIENTE BIT NOT NULL,
	DS_LATITUDE FLOAT NULL,
	DS_LONGITUDE FLOAT NULL,
	EN_RUA VARCHAR(100) NULL,
	EN_NR_EN VARCHAR(10) NULL,
	NM_BAIRRO VARCHAR(30) NULL,
	NR_CEP VARCHAR(8) NULL,
	TIPO_ENDERECO_COMP_DEF BIT NOT NULL,
	REDE_ENSINO INT NULL,
	CD_ESCOLA_COMP_DEF INT NULL,
	CD_UNIDADE_COMP_DEF INT NULL,
	DATA_INSCRICAO VARCHAR(8) NULL,
	DATA_NASCTO VARCHAR(8) NULL,
	NOME_ALUNO VARCHAR(100) NULL,
	NOME_MAE VARCHAR(100) NULL,
	NOME_PAI VARCHAR(100) NULL,
	SEXO VARCHAR(1) NULL,
	IRMAO BIT NOT NULL,
	GEMEO BIT NOT NULL,
	ESCOLA_ALOCACAO INT NULL,
	DATA_COMP_DEF VARCHAR(8) NULL,
	HORA_COMP_DEF VARCHAR(6) NULL,
	FLAG_COMP_DEF BIT NOT NULL,
	SITUACAO_COMP_DEF VARCHAR(100) NOT NULL,
	INTERESSE_INTEGRAL BIT NOT NULL
)

-- Vagas remanescentes do município
CREATE TABLE CADALUNOS..TB_REL_COMPAT_MUN_VAGAS (
	CD_ESCOLA INT NOT NULL,
	CD_TIPO_ENSINO INT NOT NULL,
	NR_SERIE INT NOT NULL,
	CD_UNIDADE INT NOT NULL,
	TURMAS INT NOT NULL,
	TOTAL_VAGAS INT NOT NULL,
	VAGAS INT NOT NULL,
	CONSTRAINT PK_REL_COMPAT_MUN_VAGAS PRIMARY KEY CLUSTERED (CD_ESCOLA ASC, CD_TIPO_ENSINO ASC, NR_SERIE ASC)
)

-- Dados da compatibilização
CREATE TABLE CADALUNOS..TB_REL_COMPAT_REAL (
	ID_REL_COMPAT_REAL BIGINT NOT NULL IDENTITY PRIMARY KEY,
	CD_DIRETORIA INT NOT NULL,
	NM_DIRETORIA VARCHAR(60) NOT NULL,
	CD_DNE INT NOT NULL,
	NM_MUNICIPIO VARCHAR(40) NOT NULL,
	CD_REDE_ENSINO INT NOT NULL,
	DS_REDE_ENSINO VARCHAR(60) NULL,
	CD_ESCOLA INT NOT NULL,
	NM_COMPLETO_ESCOLA VARCHAR(100) NOT NULL,
	CD_UNIDADE INT NOT NULL,
	CD_TIPO_ENSINO INT NOT NULL,
	NM_TIPO_ENSINO VARCHAR(60) NOT NULL,
	NR_SERIE INT NOT NULL,
	CD_TURMA INT NOT NULL,
	NR_CLASSE INT NOT NULL,
	CD_ALUNO INT NOT NULL,
	NM_ALUNO VARCHAR(100) NOT NULL,
	NR_RA VARCHAR(12) NOT NULL,
	ID_FICHA INT NOT NULL,
	ID_FASE INT NOT NULL,
	DEFICIENTE BIT NOT NULL,
	CD_IRMAO INT NOT NULL,
	GEMEO BIT NOT NULL,
	ALOCADO BIT NOT NULL,
	MOTIVO VARCHAR(100) NOT NULL
)

CREATE TABLE CADALUNOS.DBO.TB_REL_COMPAT_EXCLUSAO_DESLOCAMENTO (
	ID_REL_COMPAT_EXCLUSAO_DESLOCAMENTO BIGINT IDENTITY(1,1) NOT NULL,
	CD_MATRICULA_ALUNO BIGINT NOT NULL,
	CD_ALUNO INT NOT NULL,
	ID_RODADA INT NOT NULL,
PRIMARY KEY CLUSTERED 
(
	ID_REL_COMPAT_EXCLUSAO_DESLOCAMENTO ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]


CREATE TABLE CADALUNOS.DBO.TB_REL_COMPAT_TRANSFERENCIA_MOVIMENTACAO (
	ID_REL_COMPAT_TRANSFERENCIA_MOVIMENTACAO BIGINT IDENTITY(1,1) NOT NULL,
	CD_MATRICULA_ALUNO BIGINT NOT NULL,
	CD_ALUNO INT NOT NULL,
	ID_RODADA INT NOT NULL,
PRIMARY KEY CLUSTERED 
(
	ID_REL_COMPAT_TRANSFERENCIA_MOVIMENTACAO ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]