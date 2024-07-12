import os
import re
from neo4j import GraphDatabase
from dotenv import load_dotenv

load_dotenv()


class CreateGraph:
    def __init__(self, uri, username, password):
        self.uri = uri
        self.username = username
        self.password = password
        self.driver = None

    def conectar_neo4j(self):
        try:
            self.driver = GraphDatabase.driver(
                self.uri, auth=(self.username, self.password)
            )
            return self.driver.session()
        except Exception as e:
            print(f"Erro ao conectar ao Neo4j: {e}")
            return None

    def criar_grafo_neo4j(self, session, nome_arquivo, funcoes, codigo_arquivo):
        try:
            session.run(
                "CREATE (:Arquivo {nome: $nome_arquivo, codigo: $codigo_arquivo})",
                nome_arquivo=nome_arquivo,
                codigo_arquivo=codigo_arquivo,
            )

            for nome_funcao, codigo_funcao in funcoes:
                session.run(
                    """
                    MATCH (a:Arquivo {nome: $nome_arquivo})
                    CREATE (a)-[:CONTEM]->(:Funcao {nome: $nome_funcao, codigo: $codigo_funcao})
                """,
                    nome_arquivo=nome_arquivo,
                    nome_funcao=nome_funcao,
                    codigo_funcao=codigo_funcao,
                )

            print(f"Grafo criado com sucesso para o arquivo {nome_arquivo}!")
        except Exception as e:
            print(f"Erro ao criar grafo no Neo4j: {e}")


class CSharpCodeExtractor:
    def __init__(self):
        pass

    def extrair_funcoes_cs(self, caminho_arquivo):
        try:
            with open(caminho_arquivo, "r", encoding="utf-8") as arquivo:
                codigo = arquivo.read()

            padrao_funcao = re.compile(
                r"\b(?:public|private|protected|internal|static|virtual|abstract|sealed|override)\s+\w+\s+\w+\s*\(.*?\)\s*{",
                re.DOTALL,
            )

            padrao_propriedade = re.compile(
                r"\b(?:public|private|protected|internal|static|virtual|abstract|sealed|override)\s+\w+\s+\w+\s*{.*?}",
                re.DOTALL,
            )

            funcoes = []

            matches_funcoes = padrao_funcao.finditer(codigo)
            for match in matches_funcoes:
                inicio = match.start()
                pos = match.end()

                # Usando pilha para encontrar o final da função
                pilha = []
                pilha.append("{")

                while pos <= len(codigo) and len(pilha) != 0:
                    pos += 1
                    if codigo[pos] == "{":
                        pilha.append("{")
                    elif codigo[pos] == "}":
                        try:
                            pilha.pop()
                        except:
                            break
                nome_funcao = re.search(
                    r"\b(?:public|private|protected|internal|static|virtual|abstract|sealed|override)\s+\w+\s+(\w+)\s*\(.*?\)",
                    codigo[inicio:pos],
                ).group(1)
                funcao = codigo[inicio : pos + 1]
                funcoes.append((nome_funcao, funcao))

            matches_propriedades = padrao_propriedade.finditer(codigo)
            for match in matches_propriedades:
                inicio = match.start()
                pos = match.end()
                nome_propriedade = re.search(
                    r"\b(?:public|private|protected|internal|static|virtual|abstract|sealed|override)\s+(\w+)\s+\w+\s*{",
                    codigo[inicio:pos],
                ).group(1)
                propriedade = codigo[inicio : pos + 1].strip()
                funcoes.append((nome_propriedade, propriedade))

            return funcoes, codigo  # Retorna também o código completo do arquivo

        except Exception as e:
            print(f"Erro ao extrair funções do arquivo {caminho_arquivo}: {e}")
            return [], ""

    def extrair_funcoes_query(self, caminho_arquivo):
        try:
            with open(caminho_arquivo, "r", encoding="utf-8") as arquivo:
                codigo = arquivo.read()

            padrao_query = re.compile(
                r'\bpublic\s+static\s+readonly\s+string\s+(\w+)\s*=\s*@?"((?:[^"]|\\")*?)";',
                re.DOTALL,
            )

            matches = padrao_query.finditer(codigo)
            funcoes = []

            for match in matches:
                nome_funcao = match.group(1)
                funcao = match.group(2).strip()
                funcoes.append((nome_funcao, funcao))

            return funcoes, codigo

        except Exception as e:
            print(f"Erro ao extrair queries do arquivo {caminho_arquivo}: {e}")
            return []

    def listar_arquivos_cs(self, diretorio):
        try:
            arquivos_cs = []
            for raiz, diretorios, arquivos in os.walk(diretorio):
                for arquivo in arquivos:
                    if arquivo.endswith(".cs"):
                        caminho_arquivo = os.path.join(raiz, arquivo)
                        arquivos_cs.append(caminho_arquivo)
            return arquivos_cs

        except Exception as e:
            print(f"Erro ao listar arquivos no diretório {diretorio}: {e}")
            return []

    def extrair_e_salvar_funcoes(self, diretorio_entrada, diretorio_saida):
        try:
            arquivos_cs = self.listar_arquivos_cs(diretorio_entrada)

            for caminho_arquivo in arquivos_cs:
                nome_arquivo = os.path.basename(caminho_arquivo)
                caminho_saida_txt = os.path.join(diretorio_saida, f"{nome_arquivo}.txt")

                if "Query.cs" in caminho_arquivo:
                    funcoes, codigo_arquivo = self.extrair_funcoes_query(
                        caminho_arquivo
                    )
                else:
                    funcoes, codigo_arquivo = self.extrair_funcoes_cs(caminho_arquivo)

                with open(caminho_saida_txt, "w", encoding="utf-8") as arquivo_saida:
                    arquivo_saida.write(f"Arquivo: {nome_arquivo}\n\n")
                    arquivo_saida.write(f"Código:\n{codigo_arquivo.strip()}\n\n")
                    for nome_funcao, funcao in funcoes:
                        arquivo_saida.write(f"Função: {nome_funcao}\n")
                        arquivo_saida.write(funcao.strip() + "\n")
                        arquivo_saida.write("=" * 40 + "\n")

                print(f"Funções extraídas e salvas para {nome_arquivo}.txt")

                # Chamar criação de grafo no Neo4j
                create_graph = CreateGraph(
                    os.getenv("NEO4J_URI"),
                    os.getenv("NEO4J_USERNAME"),
                    os.getenv("NEO4J_PASSWORD"),
                )
                session = create_graph.conectar_neo4j()
                if session:
                    create_graph.criar_grafo_neo4j(
                        session, nome_arquivo, funcoes, codigo_arquivo
                    )
                    session.close()

        except Exception as e:
            print(f"Erro ao extrair e salvar funções: {e}")


def processar_codigo_csharp(diretorio_entrada, diretorio_saida):
    try:
        extractor = CSharpCodeExtractor()
        extractor.extrair_e_salvar_funcoes(diretorio_entrada, diretorio_saida)
    except Exception as e:
        print(f"Erro ao processar código C#: {e}")


# Exemplo de uso
# def main():
#     try:
#         diretorio_entrada = 'backend/upload/See.Sed.FichaAluno.Compatibiarra'
#         diretorio_saida = 'backend/outputs'
#
#         processar_codigo_csharp(diretorio_entrada, diretorio_saida)
#
#     except Exception as e:
#         print(f"Erro no processo principal: {e}")
