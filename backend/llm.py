from dotenv import load_dotenv
from langchain_community.vectorstores import FAISS
from langchain_openai import OpenAIEmbeddings
from backend.fluxograma import criar_fluxograma
from backend.agents import *
from datetime import datetime
import os

embeddings = OpenAIEmbeddings()
database_path = "faiss_index"
history = []

def update_history(history, user_message, ai_response):
    # Adiciona a nova interação ao histórico
    history.append(("human", user_message))
    history.append(("ai", ai_response))
    
    # Mantém apenas as últimas duas interações
    if len(history) > 4:
        history = history[-4:]
    
    return history

def format_history_for_prompt(history):
    history_str = ""
    for source, message in history:
        if source == "human":
            history_str += f"Usuário: {message}\n"
        elif source == "ai":
            history_str += f"Assistente: {message}\n"
    return history_str

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
def get_agent_for_question(pergunta):
    global history
    formatted_history = format_history_for_prompt(history)
    print(history, formatted_history)
    response = agent_inicial.invoke({"historico": formatted_history, "user_question": pergunta})
    response = response['answer']

    if "arquivo" in response:
        return 1
    elif "fluxograma" in response:
        return 2
    elif "regras" in response:
        return 3
    elif "fluxograma" in response and "regras" in response:
        return 4
    else:
        return 0
        
def chain_invoke(pergunta, docs):
   global history
   formatted_history = format_history_for_prompt(history)
   retriever = function_load_faiss_index()
   escolha = get_agent_for_question(pergunta)
   if escolha == 0: #sem prompt especial
       answer = normal_agente.invoke({"user_question": pergunta, "history": formatted_history})
       answer = answer['answer']
   elif escolha == 1: #prompt arquivo
      if database_exists(database_path):
                answer = agent_consulta_vetorial.invoke({"user_question": pergunta})
                # Consulta ao banco vetorial
                answer = answer['answer']
                relevant_docs = retriever.invoke(answer)
                context = "\n".join([doc.page_content for doc in relevant_docs])
                if len(context) > 22000:
                    print("Diminuiu contexto")
                    context = context[:22000]
      else:
        context = "Ainda não tem arquivos vetorizados avise o usuario."
      answer = agent_arquivo.invoke({"user_question": pergunta, "docs": context, "history": formatted_history})
      answer = answer['answer']
   elif escolha == 2: #prompt fluxograma
        answer = agent_fluxograma.invoke({"codigo": docs, "user_question": pergunta, "history": formatted_history})
        answer = answer['answer']
        print("Anser1: ", answer)
        answer = formata_resposta.invoke({"pergunta": answer})
        answer = answer['answer']
        print("Anser2: ", answer)
        nome_fluxograma = "fluxograma" + datetime.now().strftime("%Y%m%d_%H%M%S")
        criar_fluxograma(nome_fluxograma,answer)
        caminho_pdf = f"/download/{nome_fluxograma}.pdf"
        nome_fluxograma_com_extensao = nome_fluxograma + ".pdf"
        answer =  f"\n\nClique <a href='{caminho_pdf}' download='{nome_fluxograma_com_extensao}'> aqui </a> para baixar o fluxograma"
   elif escolha == 3: #prompt regras
        answer_regras = agent_regras_negocios.invoke({"codigo": docs, "history": formatted_history})
        answer_regras = answer_regras['answer']
        answer = answer_regras
   elif escolha == 4: #prompt fluxograma e regras
        answer = agent_fluxograma.invoke({"codigo": docs, "user_question": pergunta, "history": formatted_history})
        answer = answer['answer']
        answer = formata_resposta.invoke({"pergunta": answer})
        answer = answer['answer']
        nome_fluxograma = "fluxograma" + datetime.now().strftime("%Y%m%d_%H%M%S")
        criar_fluxograma(nome_fluxograma,answer)
        caminho_pdf = f"/download/{nome_fluxograma}.pdf"
        nome_fluxograma_com_extensao = nome_fluxograma + ".pdf"
        answer = agent_regras_negocios.invoke({"codigo": docs, "history": formatted_history})
        answer = answer['answer']
        answer +=  f"\n\nClique <a href='{caminho_pdf}' download='{nome_fluxograma_com_extensao}'> aqui </a> para baixar o fluxograma"
   try:
    history = update_history(history,pergunta, answer)
   except Exception as e:
       history = []
   return answer
   