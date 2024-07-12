from langchain.chains import GraphCypherQAChain
from langchain_community.graphs import Neo4jGraph
from langchain_openai import ChatOpenAI
from dotenv import load_dotenv
import codecs
import os

load_dotenv()


class QueryProcessor:
    def __init__(self):
        uri = os.getenv("NEO4J_URI")
        username = os.getenv("NEO4J_USERNAME")
        password = os.getenv("NEO4J_PASSWORD")

        self.graph = Neo4jGraph(url=uri, username=username, password=password)

    def process_query(self, query):
        # Atualizar o esquema do banco de dados e imprimir o esquema
        self.graph.refresh_schema()
        print(self.graph.schema)

        # Criar a cadeia de perguntas e respostas Cypher
        chain = GraphCypherQAChain.from_llm(
            ChatOpenAI(model="gpt-3.5-turbo", temperature=0),
            graph=self.graph,
            return_direct=True,
        )

        # Executar a consulta
        result_str = ""
        # Tratando erro na consulta
        try:
            result = chain.invoke({"query": query})
            # Tratando erro na formatação
            try:
                for item in result.get("result", []):
                    result_str += "Item:\n"
                    for key, value in item.items():
                        if isinstance(value, str):
                            # Decodificar sequências de escape
                            value = codecs.decode(value, "unicode_escape")
                        result_str += f"{key}: {value}\n"
                    result_str += "\n"
            except Exception as e:
                result_str = str(result["result"])
        except Exception as e:
            result_str = "Ocorreu um erro ao tratar a consulta: " + str(e)

        return result_str
