from langchain_core.prompts import ChatPromptTemplate, MessagesPlaceholder

inicial_prompt = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            History memory: {historico}
            ---------------------------------
            Sua tarefa é identificar qual dos prompts a pergunta do usuário irá utilizar.
            Existem três prompts: prompt_arquivo, prompt_fluxograma, e prompt_regras. Você pode usar um deles, nenhum ou combinar prompt_regras e prompt_fluxograma juntos.
            - Fluxograma de código: use prompt_fluxograma.
            - Regras de negócios: use prompt_regras.
            - Requisitos de negócio: use prompt_arquivo.
            Retorne apenas o nome do prompt escolhido. Se nenhum for aplicável, retorne apenas "nenhum".
            Pergunta: {user_question}
            """,
        )
    ]
)


prompt_arquivo = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            History memory: {history}
            ---------------------------------
            Sua principal tarefa é receber um documento e, a partir dele, gerar os requisitos de negócios
            Estrutura dos Requisitos de Negócio:
            Utilize a seguinte estrutura (adicionar, remover ou renomear tópicos se necessário):

            Requisitos de Negócios 

            1° - ITINERÁRIOS FORMATIVOS
            [Descrição]
            [Regras de negócios geradas]

            2° - TRANSFERÊNCIA 
            [Descrição]
            [Regras de negócios geradas]

            3° - DEFINIDOS
            [Descrição]
            [Regras de negócios geradas]

            4° - ESTUDANTES FORA DA REDE
            [Descrição]
            [Regras de negócios geradas]

            Responda a seguinte pergunta: {user_question}
            Usando o seguinte contexto: {docs}

            Suas respostas devem ser detalhadas e completas.
            """,
        )
    ]
)


prompt_consulta_vetorial = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            A pergunta do usuário é: '{user_question}'.
            Gere uma string que melhore a consulta para encontrar o contexto desejado no banco vetorial. Retorne apenas a string.
            """,
        )
    ]
)

prompt_fluxograma = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            History memory: {history}
            ---------------------------------
            Seu objetivo principal é receber uma pergunta do usuário e um código, interpretá-lo e abstrair o entendimento do processo que ele constrói, retornando 
            um fluxograma de alto nível do processo de forma simples e compreensível pelo usuário. Apenas retorne a especificação textual do fluxograma.
            
            Usaremos nós e conexões para desenhar textualmente o fluxograma.
            
            A estrutura dos nós é:
            NomeNó @@ SiglaNó @@ SaidaGerado (esse último é opcional, apenas caso tenha)
            
            A estrutura das conexões é:
            SiglaNó1 @@ SiglaNó2 @@ [Condição] (esse último é opcional, apenas caso tenha)
            
            Estruture o fluxograma com os seguintes elementos:
            - Um ponto inicial (Início)
            - Um ou mais processos intermediários
            - Um ponto final (Fim)
            
            Utilize o seguinte formato para o fluxograma:

            Começo Nós

            Início @@ A
            Processo X @@ B
            Processo Y @@ C
            Processo Z @@ D
            Fim @@ E

            Fim Nós

            Começo Conexões

            A @@ B
            B @@ C
            C @@ D
            D @@ E

            Fim Conexões

            Aqui está um exemplo adicional para maior clareza:

            Começo Nós

            Início @@ A
            Carregar banco de dados @@ B
            Processar dados @@ C
            Gerar relatório @@ D
            Fim @@ E

            Fim Nós

            Começo Conexões

            A @@ B
            B @@ C
            C @@ D
            D @@ E

            Fim Conexões

            Pergunta: {user_question}
            Código: {codigo}
            """
        )
    ]
)


prompt_fluxograma_geral = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            Seu objetivo principal é receber um resumo sobre um projeto de software, interpretá-lo e abstrair o entendimento do processo que ele constrói, retornando o desenho do processo.

            Retorne o desenho do processo de forma compreensível pelo usuário, detalhando e explicitando os caminhos do processo.

            Responda no seguinte formato:

            Começo Nós

            Início @@ A
            Processo @@ BA
            Decisão @@ C @@ [Saida Gerada caso tenha]
            Processo @@ D
            Fim @@ E

            Fim @@ Nós

            Começo Conexões

            A @@ BA
            BA @@ C
            C @@ D [Condição verdadeira]
            C @@ BA [Condição falsa]
            D @@ E

            Fim Conexões

            Aqui está um exemplo adicional para maior clareza:

            Começo Nós

            Início @@ A
            Processo Ler dados @@ B
            Decisão Dados @@ C
            Processo @@ D
            Processo Mostrar @@ E
            Fim @@ F

            Fim Nós

            Começo Conexões

            A @@ B
            B @@ C
            C @@ D @@ [Sim]
            C @@ E @@ [Não]
            D @@ F
            E @@ A

            Fim Conexões
            
            
            Adicione mais nós e conexões conforme necessário, seguindo o mesmo formato.
            Resumo: {resumo}
            """,
        )
    ]
)


prompt_regras_negocios = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            History memory: {history}
            ---------------------------------
            Seu objetivo principal é receber um código, interpretá-lo e abstrair o entendimento das regras de negócio que ele implementa.

            Definição de regras de negócios: As regras de negócio “traduzem” um contexto do negócio para o produto ou serviço; descrevem como se espera que o produto se comporte: condições, restrições, gatilhos, etc.

            Comece com o título REGRAS DE NEGÓCIO, seguido pelas regras implementadas.

            Seja detalhado e verboso.

            Código: {codigo}
            """,
        )
    ]
)


prompt_verifica_resposta = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            Verifique se a pergunta está formatada exatamente igual ao exemplo, sem formatações markdown ou qualquer outra coisa a mais.

            Caso esteja, retorne a pergunta sem alteração.
            Caso não esteja, retorne a reposta formatada com as formatações corretas, não alterendo o conteudo da pergunta.

            Exemplo:

            Começo Nós

            Início @@ A
            Processo @@ BA
            Decisão @@ C @@ [Saida Gerada caso tenha]
            Processo @@ D
            Fim @@ E

            Fim @@ Nós

            Começo Conexões

            A @@ BA
            BA @@ C
            C @@ D [Condição verdadeira]
            C @@ BA [Condição falsa]
            D @@ E

            Fim Conexões

            Aqui está um exemplo adicional para maior clareza:

            Começo Nós

            Início @@ A
            Processo Ler dados @@ B
            Decisão Dados @@ C
            Processo @@ D
            Processo Mostrar @@ E
            Fim @@ F

            Fim Nós

            Começo Conexões

            A @@ B
            B @@ C
            C @@ D @@ [Sim]
            C @@ E @@ [Não]
            D @@ F
            E @@ A

            Fim Conexões

            Pergunta: {pergunta}
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

            Formato do Resumo:
            - O resumo deve ser claro e conciso, organizado em seções distintas para cada aspecto abordado (propósito, funções, processos, lógica, dependências).
            - Certifique-se de que o resumo ajude na compreensão do arquivo, permitindo uma análise aprofundada de suas funcionalidades e integrações com outros componentes do projeto.

            Exemplo:

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

            Arquivo: {arquivo}
            """,
        )
    ]
)

prompt_normal = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            History memory: {history}
            ---------------------------------
            Sua principal tarefa é responder a pergunta do usuário de maneira clara e completa, utilizando as informações disponíveis.

            Pergunta: {user_question}
            """,
        )
    ]
)