from dotenv import load_dotenv
from langchain_community.vectorstores import FAISS
from langchain_openai import OpenAIEmbeddings
from backend.fluxograma import criar_fluxograma
from backend.agents import *
from datetime import datetime
import os

embeddings = OpenAIEmbeddings()
database_path = "faiss_index"


def database_exists(database_path):
    return os.path.exists(database_path)

def function_load_faiss_index():
    retriever = None
    if database_exists(database_path):
        # Carregue o banco de dados vetorizado (FAISS)
        faiss_index = FAISS.load_local(
            "faiss_index", embeddings, allow_dangerous_deserialization=True
        )
        retriever = faiss_index.as_retriever()
    return retriever

# Function to get appropriate agent
def get_agent_for_question(pergunta, history):
    print(history)
    response = agent_inicial.invoke({"history": history, "user_question": pergunta})
    response = response['answer']
    print(response)
    if "arquivo" in response:
        return 1
    elif "fluxograma" in response:
        return 2
    elif "regras" in response:
        return 3
    elif "codigo" in response:
        return 4
    else:
        return 0
        
def chain_invoke(pergunta, docs, history):
    retriever = function_load_faiss_index()
    escolha = get_agent_for_question(pergunta, history)
    
    if escolha == 0:  # sem prompt especial
        answer = normal_agente.invoke({"user_question": pergunta, "history": history})
        answer = answer['answer']
    
    elif escolha == 1:  # prompt arquivo
        if database_exists(database_path):
            answer = agent_consulta_vetorial.invoke({"user_question": pergunta})
            # Consulta ao banco vetorial
            answer = answer['answer']
            relevant_docs = retriever.invoke(answer)
            context = "\n".join([doc.page_content for doc in relevant_docs])
            if len(context) > 30000:
                print("Diminuiu contexto")
                context = context[:30000]
        else:
            context = "Ainda não tem arquivos vetorizados avise o usuario."
        print(context)
        answer = agent_arquivo.invoke({"user_question": pergunta, "docs": context, "history": history})
        answer = answer['answer']
        
    elif escolha == 2:  # prompt fluxograma
        answer_inicial = agent_analise_fluxo.invoke({"code": docs, "user_question": pergunta})
        answer_inicial = answer_inicial['answer']
        answer = agent_fluxograma.invoke({"flow_description": answer_inicial, "user_question": pergunta})
        answer = answer['answer']
        answer = formata_resposta.invoke({"user_question": answer})
        answer = answer['answer']
        nome_fluxograma = "fluxograma" + datetime.now().strftime("%Y%m%d_%H%M%S")
        try:
            criar_fluxograma(nome_fluxograma, answer)
        except:
            print("Erro ao gerar o fluxograma")
        caminho_pdf = f"/download/{nome_fluxograma}.pdf"
        nome_fluxograma_com_extensao = nome_fluxograma + ".pdf"
        answer = f"\n\nClique <a href='{caminho_pdf}' download='{nome_fluxograma_com_extensao}'> aqui </a> para baixar o fluxograma"
        answer_inicial = "**Definição do fluxograma**: \n\n" + answer_inicial + answer
        answer = answer_inicial
    
    elif escolha == 3:  # prompt regras
        answer_regras = agent_regras_negocios.invoke({"code": docs, "history": history, "user_question": pergunta})
        answer_regras = answer_regras['answer']
        answer = answer_regras
        
        # Salvar a resposta em regras.txt
        with open("regras.txt", "w", encoding='utf-8') as file:
            file.write(answer)
    
    elif escolha == 4:  # prompt analise codigo
        answer = alteracao_agent.invoke({
            "user_question": pergunta,
            "code": docs
        })
        answer = answer['answer']

    return answer