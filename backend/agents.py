from langchain.chains import LLMChain
from langchain_openai import ChatOpenAI
from backend.prompt import *
from dotenv import load_dotenv
import os
from langchain_google_genai import ChatGoogleGenerativeAI
from google.generativeai.types.safety_types import HarmBlockThreshold, HarmCategory

load_dotenv()

# # # Definiçao do LLM

safety_settings = {
    HarmCategory.HARM_CATEGORY_DANGEROUS_CONTENT: HarmBlockThreshold.BLOCK_NONE, 
    HarmCategory.HARM_CATEGORY_HATE_SPEECH: HarmBlockThreshold.BLOCK_NONE,
    HarmCategory.HARM_CATEGORY_HARASSMENT: HarmBlockThreshold.BLOCK_NONE,
    HarmCategory.HARM_CATEGORY_SEXUALLY_EXPLICIT: HarmBlockThreshold.BLOCK_NONE,
}

llm = ChatGoogleGenerativeAI(
    model="gemini-1.5-flash-latest",
    temperature=0.5,
    safety_settings=safety_settings,
)

llm_fluxo = ChatGoogleGenerativeAI(
    model="gemini-1.5-flash-latest",
    temperature=0,
    safety_settings=safety_settings,
)


# llm = ChatOpenAI(
#     model="gpt-4o",
#     temperature=1,
#     max_tokens=None,
#     timeout=None,
#     max_retries=3,
#     api_key=os.getenv("OPENAI_API_KEY"),
# )

# llm_fluxo = ChatOpenAI(
#     model="gpt-4o",
#     temperature=0.1,
#     max_tokens=None,
#     timeout=None,
#     max_retries=3,
#     api_key=os.getenv("OPENAI_API_KEY"),
# )

#Criação dos agentes
agent_inicial = LLMChain(llm=llm, prompt=inicial_prompt, output_key="answer")

agent_arquivo = LLMChain(llm=llm, prompt=prompt_arquivo, output_key="answer")

agent_consulta_vetorial = LLMChain(llm=llm, prompt=prompt_consulta_vetorial, output_key="answer")

agent_fluxograma = LLMChain(llm=llm_fluxo, prompt=prompt_fluxograma, output_key="answer")

agent_analise_fluxo = LLMChain(llm=llm_fluxo, prompt=prompt_analise_fluxo, output_key="answer")

agent_regras_negocios = LLMChain(llm=llm, prompt=prompt_regras_negocios, output_key="answer",)

formata_resposta = LLMChain(llm=llm_fluxo, prompt=prompt_verifica_resposta, output_key="answer")

normal_agente = LLMChain(llm=llm, prompt=prompt_normal, output_key="answer")

alteracao_agent = LLMChain(llm=llm, prompt=alteracao_prompt, output_key="answer")

# agent_resume_project = LLMChain(llm=llm, prompt=project_resume_prompt, output_key="answer")
# agent_fluxograma_geral = LLMChain(llm=llm, prompt=prompt_fluxograma_geral, output_key="answer")
