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

            Requisitos de Negócios\n

            1° - ITINERÁRIOS FORMATIVOS\n
            [Descrição]\n
            [Requisitos de negócios geradas]\n

            2° - TRANSFERÊNCIA\n
            [Descrição]\n
            [Requisitos de negócios geradas]\n

            3° - DEFINIDOS\n
            [Descrição]\n
            [Requisitos de negócios geradas]\n

            4° - ESTUDANTES FORA DA REDE\n
            [Descrição]\n
            [Requisitos de negócios geradas]\n

            Responda a seguinte pergunta: {user_question}\n
            Usando o seguinte contexto: {docs}\n

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
prompt_analise_fluxo = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            Seu objetivo é analisar a pergunta do usuário e o código fornecido, identificar o fluxo principal de execução e os dados gerados, e estruturar essa informação de maneira clara e consistente para ser usada na geração de um fluxograma textual posteriormente.

            Siga as instruções abaixo para realizar a análise:

            1. Identifique a função `main` no código e comece a análise a partir dela. Se a pergunta do usuário especificar uma função diferente, comece a análise a partir dessa função.
            2. Liste todos os processos ou etapas que ocorrem dentro dessa função e as funções chamadas por ela em ordem sequencial.
            3. Inclua quaisquer condições ou bifurcações importantes.
            4. Identifique e liste qualquer arquivo ou dado gerado durante a execução do código.
            5. Tente não criar muitos processos ou etapas. Se você encontrar um processo ou etapa que não seja necessário, ignore-o ou adcione-o como ação em um processo ou etapa anterior.
            
            Estruture sua resposta da seguinte maneira:

            **Objetivo principal:**
            - Descreva o objetivo principal do código em uma frase clara.

            **Principais processos/etapas:**
            1. [Nome do primeiro processo]
                - [Descreva as ações ou processos realizados neste processo]
            2. [Nome do segundo processo]
                - [Descreva as ações ou processos realizados neste processo]
            3. ...

            **Condições importantes:**
            - [Descreva qualquer condição ou bifurcação importante]

            **Arquivos ou dados gerados:**
            - [Descreva qualquer arquivo ou dado gerado]

            Exemplos:

            **Exemplo 1**

            **Pergunta:**
            Como funciona o processo de cálculo de notas dos alunos?

            **Código:**
            ```
            def main():
                students = get_students()
                grades = calculate_grades(students)
                final_grades = assign_final_grades(grades)
                save_results(final_grades)
                print("Process completed")

            def calculate_grades(students):
                grades = []
                for student in students:
                    total = sum(student['scores'])
                    grades.append(student_id: student['id'], 'total': total)
                return grades

            def assign_final_grades(grades):
                final_grades = []
                for grade in grades:
                    if grade['total'] >= 90:
                        final_grades.append('student_id': grade['student_id'], 'grade': 'A')
                    elif grade['total'] >= 80:
                        final_grades.append('student_id': grade['student_id'], 'grade': 'B')
                    elif grade['total'] >= 70:
                        final_grades.append('student_id': grade['student_id'], 'grade': 'C')
                    else:
                        final_grades.append('student_id': grade['student_id'], 'grade': 'F')
                return final_grades

            def save_results(final_grades):
                with open('final_grades.txt', 'w') as file:
                    for grade in final_grades:
                        file.write(f"Student grade['student_id']: grade['grade']\n")
            ```

            **Resposta:**

            **Objetivo principal:**
            - O código calcula as notas finais dos alunos com base em suas pontuações e salva os resultados em um arquivo.

            **Principais processos/etapas:**
            1. Obtenção de Dados dos Alunos
                - Carrega a lista de alunos e suas pontuações.
            2. Cálculo de Notas
                - Calcula o total das pontuações de cada aluno.
            3. Atribuição de Notas Finais
                - Atribui uma nota final (A, B, C ou F) com base no total das pontuações.
            4. Salvamento dos Resultados
                - Salva as notas finais em um arquivo de texto.

            **Condições importantes:**
            - Se o total de pontuações for maior ou igual a 90, o aluno recebe nota A.
            - Se o total de pontuações for maior ou igual a 80, o aluno recebe nota B.
            - Se o total de pontuações for maior ou igual a 70, o aluno recebe nota C.
            - Se o total de pontuações for inferior a 70, o aluno recebe nota F.

            **Arquivos ou dados gerados:**
            - Arquivo de texto `final_grades.txt` contendo as notas finais dos alunos.

            Agora, analise o código a seguir e forneça a descrição de alto nível seguindo estritamente o formato acima, mas adapte conforme necessário para refletir com precisão o funcionamento do código:

            **Pergunta:**
            {user_question}

            **Código:**
            {codigo}
            """
        )
    ]
)


prompt_fluxograma = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            Seu objetivo principal é transformar a descrição do fluxo principal de um processo em um fluxograma textual com a visão macro do processo.

            **Descrição do Fluxo Principal:**

            {descricao_fluxo}

            Usaremos nós e conexões para desenhar textualmente o fluxograma.
            Use os processos/etapas como os nós nos fluxogramas.

            A estrutura dos nós é:
            NomeNó @@ SiglaNó

            A estrutura das conexões é:
            SiglaNó1 @@ SiglaNó2 @@ [Condição] (esse último é opcional, apenas caso tenha)

            Estruture o fluxograma com os seguintes elementos:
            - Um ponto inicial (Início)
            - Um ou mais processos intermediários
            - Um ponto final (Fim)

            Utilize o seguinte formato para o fluxograma:

            Começo Nós

            Início @@ A
            Processo @@ BA
            Decisão @@ C
            Processo @@ D
            Fim @@ E

            Fim @@ Nós

            Começo Conexões

            A @@ BA
            BA @@ C
            C @@ D @@ [Condição verdadeira]
            C @@ BA @@ [Condição falsa]
            D @@ E

            Fim Conexões
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
            Decisão @@ C
            Processo @@ D
            Fim @@ E

            Fim @@ Nós

            Começo Conexões

            A @@ BA
            BA @@ C
            C @@ D @@ [Condição verdadeira]
            C @@ BA @@ [Condição falsa]
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
            Histórico de memória: {history}
            ---------------------------------
            Seu objetivo principal é receber um código, interpretá-lo e abstrair o entendimento das regras de negócio que ele implementa.

            Definição de regras de negócios: As regras de negócio traduzem um contexto do negócio para o produto ou serviço; descrevem como se espera que o produto se comporte: condições, restrições, gatilhos, etc.

            Instruções:
            1. Comece com o título "REGRAS DE NEGÓCIO", seguido pelas regras implementadas.
            2. Para cada regra de negócio, descreva:
                - O que ela faz (Objetivo - PES).
                - Como ela é implementada (Detalhes do Código Relevante - AR).
                - Condições necessárias para sua execução (Critérios - Al).
                - Restrições aplicadas (Travas - R).
                - Gatilhos que disparam a regra (Gatilhos).
            3. Seja detalhado e verboso, fornecendo explicações claras e completas.

            Código: {codigo}

            Pergunta do usuário: {pergunta}
            """,
        )
    ]
)

prompt_verifica_resposta = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            Verifique se a pergunta está formatada exatamente igual ao exemplo, sem formatações markdown como por exemplo ** texto ** ou qualquer outra coisa a mais.

            Caso esteja, retorne a pergunta sem alteração.
            Caso não esteja, retorne a reposta formatada com as formatações corretas, não alterendo o conteudo da pergunta.

            Exemplo:

            Começo Nós

            Início @@ A
            Processo @@ BA
            Decisão @@ C
            Processo @@ D
            Fim @@ E

            Fim @@ Nós

            Começo Conexões

            A @@ BA
            BA @@ C
            C @@ D @@ [Condição verdadeira]
            C @@ BA @@ [Condição falsa]
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