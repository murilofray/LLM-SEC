from dotenv import load_dotenv
from langchain.chains import LLMChain
from langchain_community.vectorstores import FAISS
from langchain_openai import ChatOpenAI
from langchain_core.prompts import (ChatPromptTemplate)
from langchain_openai import OpenAIEmbeddings
from backend.fluxograma import criar_fluxograma
from datetime import datetime
from langchain_community.agent_toolkits import FileManagementToolkit
import os
from langchain_google_genai import ChatGoogleGenerativeAI
from google.generativeai.types.safety_types import HarmBlockThreshold, HarmCategory
from langchain.agents import AgentType,initialize_agent


load_dotenv()


safety_settings = {
    HarmCategory.HARM_CATEGORY_DANGEROUS_CONTENT: HarmBlockThreshold.BLOCK_NONE, 
    HarmCategory.HARM_CATEGORY_HATE_SPEECH: HarmBlockThreshold.BLOCK_NONE,
    HarmCategory.HARM_CATEGORY_HARASSMENT: HarmBlockThreshold.BLOCK_NONE,
    HarmCategory.HARM_CATEGORY_SEXUALLY_EXPLICIT: HarmBlockThreshold.BLOCK_NONE,
}

llm = ChatGoogleGenerativeAI(
    model="gemini-1.5-flash-latest",
    temperature=0.4,
    safety_settings=safety_settings
)

# llm = ChatOpenAI(
#     model="gpt-4o",
#     temperature=0.5,
#     max_tokens=None,
#     timeout=None,
#     max_retries=3,
#     api_key=os.getenv("OPENAI_API_KEY")
# )


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

inicial_prompt = ChatPromptTemplate.from_messages(
    [
        (
        "user",
        """
        Sua principal tarefa é receber uma pergunta é entender quais dos prompt essa pergunta ira usar.
        existe três prompts o prompt_arquivo, prompt_fluxograma e o prompt_regras, podendo usar nenhum ou prompt_regras e prompt_fluxograma juntos.
        tudo que envolver código, gerar fluxograma de código é do prompt_fluxograma.
        tudo que envolver regras de negocios, gerar regras de negocios a partir de um código é do prompt_regras.
        tudo que envolver gerar requisitos é do prompt_arquivo.
        retorne como resposta apenas o prompt escolhido e mais nada junto, caso seja nenhum resposta apensa nenhum.
        pergunta: {user_question}
        """,
        )
    ]
)

project_resume_prompt = ChatPromptTemplate.from_messages(
    [
        (
        "user",
        """
        Objetivo: Gerar um resumo detalhado de um arquivo específico do projeto, incluindo seu propósito, funções, processos, lógica e dependências.

        Instruções:

        1. Identificação do Arquivo: Informe o nome do arquivo que deseja resumir.
        2. Descrição do Arquivo:
              - Descreva o propósito principal do arquivo e suas principais funções.
              - Detalhe os processos e a lógica subjacente no funcionamento do arquivo.
              - Liste as dependências externas necessárias para o correto funcionamento do arquivo.
        Formato do Resumo: O resumo deve ser claro e conciso, organizado em seções distintas para cada aspecto abordado (propósito, funções, processos, lógica, dependências).
        Foco na Compreensão: Certifique-se de que o resumo ajude na compreensão do arquivo, permitindo uma análise aprofundada de suas funcionalidades e integrações com outros componentes do projeto.

        Exemplo (apenas um exemplo não precisa colocar ele na resposta):

        '''
        Propósito:
        O arquivo `arquivo.py` tem como objetivo principal realizar a manipulação de dados do usuário dentro do sistema XYZ.

        Funções:
        - `funcao_principal`: Responsável por processar dados de entrada e gerar saídas formatadas.
        - `funcao_auxiliar`: Suporta `funcao_principal` ao realizar operações de validação de dados.

        Processos e Lógica:
        Este arquivo utiliza uma abordagem de processamento sequencial para validar e processar dados de entrada, aplicando regras específicas de negócios.

        Dependências:
        - `outro_arquivo.py`: Importado para acesso a funções de utilidade.
        - `biblioteca_A`: Utilizada para manipulação de strings.
        '''

        Arquivo: {arquivo}
        """,
        )
    ]
)

prompt_arquivo = ChatPromptTemplate.from_messages(
    [
        (
        "user",
        """Sua principal tarefa é receber um documento e a partir dele, gerar os requisitos de negócios das regras de compatibilização.

        Capacidades Adicionais:
        Além de gerar os requisitos de negócios a partir dos documentos, você também pode responder a outras perguntas dos usuários sem precisar consultar os documentos.

        Estrutura dos Requisitos de Negócio
        Ao gerar os requisitos de negócios das regras de compatibilização, utilize sempre a seguinte estrutura 
        (não precisa seguir todos topicos, pode adicionar, remover ou renomear topicos a vontade é só um exemplo sobre como estruturar, coloque só os que fazem sentido):

        Requisitos de Negócios 

        1° - ITINERÁRIOS FORMATIVOS\n
         [Descrição]
         [Regras de negocios que conseguiu gerar]

        2° - TRANSFERÊNCIA \n
         [Descrição]
         [Regras de negocios que conseguiu gerar]

        3° - DEFINIDOS\n
         [Descrição]
         [Regras de negocios que conseguiu gerar]

        4° - ESTUDANTES FORA DA REDE\n
         [Descrição]
         [Regras de negocios que conseguiu gerar]

        Responda a seguinte pergunta: {user_question}
        Procurando nos seguintes documentos: {docs}

        Use somente informação do documento para responder a pergunta.

        Suas respostas devem ser bem detalhadas e verbosas.
        """,
        )
    ]
)

prompt_arquivo2 = ChatPromptTemplate.from_messages(
    [
        (
        "user",
        """
        A pergunta do usuário é: '{user_question}'.
        Gere como resposta  uma string como consulta para achar no banco FAISS o contexto desejado retorne apenas ela mais nada, se possivel melhore a string de consulta.
        """,
        )
    ]
)

prompt_filesystem = ChatPromptTemplate.from_messages(
    [
        (
        "user",
        r"""
        A pergunta do usuário é: '{user_question}'.
        SE PEDIR UM ARQUIVO ENTAO PRECISA LER O CODIGO
        Caso precise ler algum arquivo ou codigo digite diretorio: ler o arquivo nome do arquivo ou o caminho do arquivo e o nome.
        Nunca invete nada.
        Caso não precise apenas responda 'resposta'.
        Arquivos existentes: 
        Pasta: See.Sed.FichaAluno.Compatibiarra
        - packages.config
        - Program.cs
        - See.Sed.FichaAluno.Compatibiarra.csproj
        - See.Sed.FichaAluno.Compatibiarra.sln

        Pasta: Models
        - Models\Aluno.cs
        - Models\DictionaryRankeado.cs
        - Models\Email.cs
        - Models\GradeDeObjetos.cs
        - Models\Motivo.cs
        - Models\ObjetoGeocodificado.cs
        - Models\Ordem.cs
        - Models\TipoEnsinoSerie.cs
        - Models\Turno.cs
        - Models\Unidade.cs
        - Models\Vagas.cs

        Pasta: Properties
        - Properties\AssemblyInfo.cs

        Pasta: SQL
        - SQL\Compatibilizacao.sql
        - SQL\Query.cs

        Pasta: Template
        - Template\Email.html

        Pasta: Util
        - Util\Mail.cs
        """,
        )
    ]
)

prompt_fluxograma = ChatPromptTemplate.from_messages(
    [
        (
        "user",
        """Seu objetivo principal é receber um código e intepretar e abstrair o entendimento do processo como um todo, que o código constroi, e retornar o desenho do processo.\n
        Retorne o desenho do processo de acordo de forma que seja compreensível pelo usuário.\n
        Um fluxo de como o codigo funcionar.\n
        Seja detalhado e explicito sobe os caminhos do processo.\n
        Você deve retornar uma resposta com o seguinte formato (resposta sempre igual o formato do exemplo, na parte do exemplo não coloque nenhuma marcação aspas, hashtag ou qualquer coisa a mais se não a função que gera o fluxograma não funciona)\n
        Esse é apenas um exemplos pode dar outros nomes para os nós e conexões para o fluxograma.\n
        Exemplo:\n
        
        Começo Nós\n

        Início (A)\n
        Processo 1 (B) \n
        Processo 2 (C) \n
        Fim (D)\n

        Fim Nós\n

        Começo Conexões\n

        Conexões:\n
        A para B [Condição 1]\n
        B para C\n
        C para D [Condição]\n

        Fim Conexões\n

        \nSeguindo esse exemplo a cima se necessário, você pode adicionar mais nós e conexões, mas sempre seguindo o mesmo formato e substituindo os nomes dos nós pelos correspondentes no código. \n
        Código: {codigo}
        """,
        )
    ]
)

prompt_fluxograma2 = ChatPromptTemplate.from_messages(
    [
        (
        "user",
        """Seu objetivo principal é receber um resumo sobre um projeto de software e intepretar e abstrair o entendimento do processo como um todo, que o projeto constroi, e retornar o desenho do processo.\n
        Retorne o desenho do processo de acordo de forma que seja compreensível pelo usuário.\n
        Um fluxo de como o codigo funcionar.\n
        Seja detalhado e explicito sobe os caminhos do processo.\n
        Você deve retornar uma resposta com o seguinte formato (resposta sempre igual o formato do exemplo, na parte do exemplo não coloque nenhuma marcação aspas, hashtag ou qualquer coisa a mais se não a função que gera o fluxograma não funciona)\n
        Esse é apenas um exemplos pode dar outros nomes para os nós e conexões para o fluxograma.\n
        Exemplo:\n
        
        Começo Nós\n

        Início (A)\n
        Processo 1 (B) \n
        Processo 2 (C) \n
        Fim (D)\n

        Fim Nós\n

        Começo Conexões\n

        Conexões:\n
        A para B [Condição 1]\n
        B para C\n
        C para D [Condição]\n

        Fim Conexões\n

        \nSeguindo esse exemplo a cima se necessário, você pode adicionar mais nós e conexões, mas sempre seguindo o mesmo formato e substituindo os nomes dos nós pelos correspondentes no código. \n
        Resumo: {resumo}
        """,
        )
    ]
)


prompt_regras_negocios = ChatPromptTemplate.from_messages(
    [
        (
        "user",
        """Seu objetivo principal é receber um código e
        intepretar e abstrair o entendimento das regras de negócio, que o código constrói, e retornar todas as regras de negócio implementadas.
        Definição de regras de negócios: As regras de negócio “traduzem” um contexto do negócio para que este contexto (e suas necessidades) seja compreendido e aplicado no produto ou serviço; elas descrevem como se espera que o produto se comporte: condições, restrições, gatilhos, etc
        Começe com um titulo REGRAS DE NEGÓCIO, e depois as regras de negócio implementadas., seja detalhado e verboso nas regras de negócios.
        Exemplo (É só um exemplo não utilizar): 
        '''
        RNG1 – Validação de E-mail Corporativo\n
        Apenas endereços de e-mail corporativos serão aceitos para o cadastro no sistema. 
        O domínio do e-mail deve ser "@empresa.com". E-mails com domínios públicos, como "@gmail.com", "@yahoo.com" e similares, não serão aceitos.
        '''
        \n\n
        Código: {codigo}
        
        """,
        )
    ]
)

prompt_verifica_resposta = ChatPromptTemplate.from_messages(
    [
        (
        "user",
        """Veja se a pergunta está formatada exatamente igual ao  exemplo sem formatações markdown ou qualquer outra coisa a mais\n
        Caso esteja apenas retorne a pergunta sem alteração\n
        Caso não esteja, retorne o exemplo com as formatações\n
        (ATENÇÃO É PARA VERIFICAR SE ESTÁ NO MESMO FORMATO E NÃO PARA DEIXAR COM AS MESMA INFORMAÇÕES QUE O EXEMPLO)
        Exemplo:\n

        Começo Nós\n

        Início (A)\n
        Processo 1 (B) [Saida Gerada Caso Tenha]\n
        Processo 2 (C) [Saida Gerada Caso Tenha]\n
        Fim (D)\n

        Fim Nós\n

        Começo Conexões\n

        Conexões:\n
        A para B [Condição 1]\n
        B para C\n
        C para D [Condição]\n
        
        Fim Conexões\n

        \n\n
        Pergunta: {pergunta}
        
        """,
        )
    ]
)

#Criação dos agentes
agent_incial = LLMChain(llm=llm, prompt=inicial_prompt, output_key="answer")
agent_arquivo = LLMChain(llm=llm, prompt=prompt_arquivo, output_key="answer")
agent_arquivo2 = LLMChain(llm=llm, prompt=prompt_arquivo2, output_key="answer")
agent_fluxograma = LLMChain(llm=llm, prompt=prompt_fluxograma, output_key="answer")
agent_fluxograma2 = LLMChain(llm=llm, prompt=prompt_fluxograma2, output_key="answer")
agent_resume_project = LLMChain(llm=llm, prompt=project_resume_prompt, output_key="answer")
agent_regras_negocios = LLMChain(llm=llm, prompt=prompt_regras_negocios, output_key="answer")
formata_resposta = LLMChain(llm=llm, prompt=prompt_verifica_resposta, output_key="answer")
agent_file = LLMChain(llm=llm, prompt=prompt_filesystem, output_key="answer")

def chain_invoke(pergunta, docs, diretorio):
   answer_regras = chain_invoke_chat(pergunta, docs)
   return answer_regras

def chain_invoke_chat(pergunta, docs):
   retriever = function_load_faiss_index()
   answer = agent_incial.invoke({"user_question": pergunta})
   answer = answer['answer']

   print("\n\n\n" + answer)
   if 'arquivo' in answer:
      if database_exists(database_path):
                answer = agent_arquivo2.invoke({"user_question": pergunta})
                # Consulta ao banco de dados
                user_question2 = answer['answer']
                print(user_question2)
                relevant_docs = retriever.invoke(user_question2)
                context = "\n".join([doc.page_content for doc in relevant_docs])
                if len(context) > 22000:
                    print("Diminuiu contexto")
                    context = context[:22000]
      else:
        context = "Ainda não tem arquivos vetorizados avise o usuario."
      print("\n\n\n" + context)
      answer = agent_arquivo.invoke({"user_question": pergunta, "docs": context})
      answer = answer['answer']
   else:
      fluxograma = False
      if 'fluxograma' in answer:
        answer = agent_fluxograma.invoke({"codigo": docs})
        answer = answer['answer']
        answer = formata_resposta.invoke({"pergunta": answer})
        answer = answer['answer']
        nome_fluxograma = "fluxograma" + datetime.now().strftime("%Y%m%d_%H%M%S")
        criar_fluxograma(nome_fluxograma,answer)
        caminho_pdf = f"/download/{nome_fluxograma}.pdf"
        nome_fluxograma_com_extensao = nome_fluxograma + ".pdf"
        answer =  f"\n\nClique <a href='{caminho_pdf}' download='{nome_fluxograma_com_extensao}'> aqui </a> para baixar o fluxograma"
        fluxograma = True
        answer_regras = agent_regras_negocios.invoke({"codigo": docs})
        answer_regras = answer_regras['answer']
        answer = answer_regras
        if fluxograma:
            answer +=  f"\n\nClique <a href='{caminho_pdf}' download='{nome_fluxograma_com_extensao}'> aqui </a> para baixar o fluxograma"

   return answer

def chain_invoke_projeto(pergunta, docs, diretorio):
   context = ""
   retriever = function_load_faiss_index()
   toolkit=FileManagementToolkit(root_dir=diretorio)
   tools=toolkit.get_tools()
   anwer = agent_file.invoke({"user_question": pergunta})
   anwer = anwer['answer']
   print("\n\n\n" + anwer)
   if 'diretorio' in anwer.lower():
        agent_directory=initialize_agent(tools,llm,agent=AgentType.STRUCTURED_CHAT_ZERO_SHOT_REACT_DESCRIPTION,verbose=True,
                            agent_executor_kwards={"handle_parsing_erros":True})
        context = agent_directory.invoke(anwer)
        context = context['output']
        answer = agent_fluxograma.invoke({"codigo": context})
        answer = answer['answer']
        print("\n\n\n" + "saiu")
        nome_fluxograma = "fluxograma" + datetime.now().strftime("%Y%m%d_%H%M%S")
        criar_fluxograma(nome_fluxograma,answer)
        answer_regras = agent_regras_negocios.invoke({"codigo": docs})
        caminho_pdf = f"/download/{nome_fluxograma}.pdf"
        answer_regras = answer_regras['answer']
        nome_fluxograma_com_extensao = nome_fluxograma + ".pdf"
        answer_regras +=  f"\n\nClique <a href='{caminho_pdf}' download='{nome_fluxograma_com_extensao}'> aqui </a> para baixar o fluxograma"
   else:
       answer_regras = "Falhou"
   return answer_regras