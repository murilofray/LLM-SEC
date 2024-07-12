import os
from backend.llm import *
from neo4j import GraphDatabase
from dotenv import load_dotenv

load_dotenv()


def processar_e_resumir_arquivos(user_folder, user_resumo_folder):
    # Dicionário para armazenar os arquivos por pasta
    arquivos_por_pasta = {}

    # Verifica se o diretório do usuário existe
    if not os.path.exists(user_folder):
        raise ValueError(f"Diretório do usuário não encontrado: {user_folder}")

    # Inicializa a chave para a pasta principal
    arquivos_por_pasta["principal"] = []

    # Itera sobre todas as pastas e arquivos dentro do diretório do usuário
    for root, dirs, files in os.walk(user_folder):
        # Adiciona arquivos da pasta principal
        if root == user_folder:
            for file_name in files:
                file_path = os.path.join(root, file_name)
                if os.path.isfile(file_path):
                    arquivos_por_pasta["principal"].append(file_path)

        # Adiciona arquivos das subpastas
        for dir_name in dirs:
            dir_path = os.path.join(root, dir_name)
            arquivos_por_pasta[dir_name] = []
            for file_name in os.listdir(dir_path):
                file_path = os.path.join(dir_path, file_name)
                if os.path.isfile(file_path):
                    arquivos_por_pasta[dir_name].append(file_path)

    # String para armazenar o resumo concatenado
    resumo_concatenado = ""

    # Adiciona cabeçalho com todos os diretórios e arquivos
    cabecalho = "Diretórios e arquivos encontrados:\n"
    for pasta, arquivos in arquivos_por_pasta.items():
        cabecalho += f"\nPasta: {pasta}\n"
        for arquivo in arquivos:
            cabecalho += f"  - {arquivo}\n"
    cabecalho += "\n\n"

    # Adiciona o cabeçalho ao resumo concatenado
    resumo_concatenado += cabecalho

    # Processa e resume cada arquivo
    for pasta, arquivos in arquivos_por_pasta.items():
        for arquivo in arquivos:
            try:
                with open(arquivo, "r", encoding="utf-8") as f:
                    conteudo = f.read()
                    resposta = agent_resume_project.invoke({"arquivo": conteudo})
                    resumo_concatenado += (
                        resposta["answer"]
                        + "\n-----------------------------------------------------------------------------------\n"
                    )
            except Exception as e:
                print(f"Erro ao processar o arquivo {arquivo}: {str(e)}")
    try:
        output_txt_path = os.path.join(user_resumo_folder, "ResumoProjeto.txt")
        salvar_resumo_em_arquivo(resumo_concatenado, output_txt_path)
    except Exception as e:
        print(f"Erro ao salvar o resumo em um arquivo: {str(e)}")


def conectar_neo4j(uri, username, password):
    try:
        driver = GraphDatabase.driver(uri, auth=(username, password))
        return driver.session()
    except Exception as e:
        print(f"Erro ao conectar ao Neo4j: {e}")
        return None


def criar_nodo_resumo(session, resumo_texto):
    try:
        session.run(
            """
            CREATE (:ResumoProjeto {texto: $resumo_texto})
        """,
            resumo_texto=resumo_texto,
        )
        print(f"Nó de resumo criado com sucesso no Neo4j!")
    except Exception as e:
        print(f"Erro ao criar nó de resumo no Neo4j: {e}")


def salvar_resumo_em_arquivo(resumo_texto, output_path):
    # Salvar resumo em arquivo
    try:
        with open(output_path, "w", encoding="utf-8") as file:
            file.write(resumo_texto)
        print(f"Resumo salvo em {output_path}")

        # Conectar ao Neo4j e criar o nó de resumo
        session = conectar_neo4j(
            os.getenv("NEO4J_URI"),
            os.getenv("NEO4J_USERNAME"),
            os.getenv("NEO4J_PASSWORD"),
        )
        if session:
            criar_nodo_resumo(session, resumo_texto)
            session.close()
    except Exception as e:
        print(f"Erro ao salvar resumo em arquivo e/ou criar nó no Neo4j: {e}")
