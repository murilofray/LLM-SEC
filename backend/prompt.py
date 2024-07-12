from langchain_core.prompts import ChatPromptTemplate, MessagesPlaceholder

inicial_prompt = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            **Conversation History**: {history}\n
            ---------------------------------------------------------------\n
            Your task is to identify which of the prompts the user's question will use. There are four prompts: `prompt_arquivo`, `prompt_fluxograma`, `prompt_regras`, and `prompt_analise_codigo`. You can use one of them or none.

            - **Code flowchart**: Use `prompt_fluxograma` when the question refers to the structure, flow, or creation of a flowchart of the code. Examples: 'Create a flowchart of this code.'

            - **Business rules of the code**: Use `prompt_regras` when the question refers to the business rules implemented in the code. Examples: 'What business rules are enforced in this function?' or 'Describe the rules implemented in this code.'

            - **Business requirements**: Use `prompt_arquivo` when the question refers to requirements in a document and generating requirements. Examples: 'What are the requirements for this project?' or 'Generate the business requirements from this document.'

            - **Code modification based on requirements**: Use `prompt_analise_codigo` when the question refers to the need for code changes to meet requirements, demands, or necessary changes. Examples: 'What changes are needed in the code to meet these requirements?' or 'Modify the code to fulfill the new business requirements.'

            - Only use the prompts when generating something like a flowchart, business rules, requirement analysis, or code modifications. For questions that do not request generating anything, use 'nenhum.'

            Return only the name of the chosen prompt. If none apply, return only 'nenhum.'
            Question: {user_question}
            """,
        )
    ]
)

prompt_arquivo = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            **Conversation History**: {history}\n
            ---------------------------------------------------------------\n
            **Objective**: Identify and list the implementation demands for the process described in the requirements document.

            **Instructions**:

            1. **Thoroughly read the document**: Carefully review the document, paying attention to all details and sections.

            2. **Identify key sections**: Locate sections of the document that specify implementation requirements. These may include:

                - Functional descriptions
                - Technical requirements
                - Integrations with external systems
                - Data migration procedures

            3. **Extract relevant information**: For each identified section, extract information about:

                - Specific functionalities that need to be developed or configured.
                - Technologies or tools that will be used.
                - Steps required for implementation.
                - Dependencies and integrations with other systems.
                - Timelines and schedules.
                - Necessary human and material resources.
                - Acceptance criteria and success metrics.

            4. **List the demands**: Organize the demands into a clear and structured list. Each item in the list should include:

                - **Descrição**: A brief description of the demand.
                - **Origem**: The origin of the demand in the document (e.g., section or paragraph).
                - **Requerimentos**: Specific requirements and implementation steps.
                - **Passos de Implementação**: Necessary steps.
                - **Dependências**: Dependent systems or processes.
                - **Recursos Necessários**: Required human/material resources.
                - **Critérios de Aceitação**: Success metrics.

            **Formato da Lista**:

            **Demanda 1**:\n
            **Descrição**: [Brief description of the demand]\n
            **Origem**: [Section/Paragraph]\n
            **Requerimentos**: [Details of the requirements]\n
            **Passos de Implementação**: [Necessary steps]\n
            **Dependências**: [Dependent systems or processes]\n
            **Recursos Necessários**: [Human/material resources]\n
            **Critérios de Aceitação**: [Success metrics]\n

            **Demanda 2**:\n
            **Descrição**: [Brief description of the demand]\n
            **Origem**: [Section/Paragraph]\n
            **Requerimentos**: [Details of the requirements]\n
            **Passos de Implementação**: [Necessary steps]\n
            **Dependências**: [Dependent systems or processes]\n
            **Recursos Necessários**: [Human/material resources]\n
            **Critérios de Aceitação**: [Success metrics]\n

            **Practical Example**:

            Imagine the document includes a requirement to integrate a new student management module with an existing database system. The demand could be described as follows:

            **Demanda 1**:\n
            **Descrição**: Integration of the student management module with the existing database.\n
            **Origem**: Section 3.2, Paragraph 4\n
            **Requerimentos**: The module must read and write data to the existing SQL database.\n
            **Passos de Implementação**: Develop integration APIs, conduct connection tests, validate data.\n
            **Dependências**: SQL database system.\n
            **Recursos Necessários**: Developers with SQL and API experience, testing environment.\n
            **Critérios de Aceitação**: Stable connectivity, accurate data read/write operations.\n

            **Finalization**:

            After listing all the demands, review the list to ensure completeness and clarity. This list will serve as the foundation for planning and executing the process implementation.

            **Requirements Document**: {docs}
            **Question**: {user_question}

            **Answer in Portuguese Brazil**.
            """,
        )
    ]
)

prompt_consulta_vetorial = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            Question: '{user_question}'.
            Generate a string to improve the query to find the desired context in the vector database. Return only the string.
            Answer in Portuguese Brazil.
            """,
        )
    ]
)

prompt_analise_fluxo = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            **Conversation History**: {history}\n
            ---------------------------------------------------------------\n
            Your goal is to analyze the user's question and the provided code, identify the main execution flow and the generated data, and structure this information clearly and consistently to be used in generating a textual flowchart later.

            Follow the instructions below to perform the analysis:

            1. **Identify the `main` function in the code** and start the analysis from there. If the user's question specifies a different function, start the analysis from that function.
            2. **List all processes or steps** that occur within that function and the functions it calls sequentially.
            3. **Include any important conditions** or branches.
            4. **Identify and list any files or data** generated during code execution.
            5. **Try not to create too many processes or steps**. If you find an unnecessary process or step, ignore it or add it as an action in a previous process or step.

            Structure your answer as follows:

            **Objetivo principal:**
            - [Describe the main objective of the code in a clear sentence]

            **Principais Processos/Etapas:**
            1. [Name of the first process]
            - [Describe the actions or processes performed in this process]
            2. [Name of the second process]
            - [Describe the actions or processes performed in this process]
            3. ...

            **Condições importantes:**
            - [Describe any important condition or branch]

            **Arquivos ou dados gerados:**
            - [Describe any file or data generated]

            ### Autorreflexão:

            1. **Geração Inicial**:
            - Generate the initial response based on the provided code.
            2. **Revisão Crítica**:
            - Review the initial response, identify strengths and weaknesses, and propose improvements.
            3. **Iteração**:
            - Refine the initial response based on the suggestions from the critical review.
            4. **Ciclo Contínuo**:
            - Repeat the review and refinement until a satisfactory response is achieved.

            **Code**: {code}
            **Question**: {user_question}
            **Answer in Portuguese Brazil.**
            """,
        )
    ]
)

prompt_fluxograma = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            Your main goal is to transform the main process flow description into a textual flowchart with a macro view of the process.

            **Main Flow Description:**

            {flow_description}

            We will use nodes and connections to textually draw the flowchart.
            Use the processes/steps as nodes in the flowcharts.

            The node structure is:
            NodeName @@ NodeAbbreviation

            The connection structure is:
            NodeAbbreviation1 @@ NodeAbbreviation2 @@ [Condition] (this last one is optional, only if there is one)

            Structure the flowchart with the following elements:
            - A starting point (Start)
            - One or more intermediate processes
            - An end point (End)

            Use the following format for the flowchart:

            Começo Nós

            Início @@ A
            Processo @@ BA
            Decisão @@ C
            Processo @@ D
            Fim @@ E

            Fim Nós

            Começo Conexões

            A @@ BA
            BA @@ C
            C @@ D @@ [Condição verdadeira]
            C @@ BA @@ [Condição falsa]
            D @@ E

            Fim Conexões

            Answer in Portuguese Brazil.
            """,
        )
    ]
)

alteracao_prompt = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            ### Context
            You are in charge of analyzing the source code to ensure it meets the specified business requirements. Your goal is to identify any gaps between the business requirements and the implemented code. Use the provided information to suggest the necessary modifications to align the code with all business requirements.

            ### Context
            {context}

            ### User Question
            {user_question}

            ### Task
            Based on the provided business requirements and the source code, identify which requirements are not being met. Then, suggest modifications to the source code to meet all business requirements.

            ### Expected Output
            1. **Analysis**: Identify the business requirements that are not being met by the source code.
            2. **Modification Suggestions**: Provide specific modifications to the source code that address the identified gaps.

            ### Example Output

            **Analysis**
            Unmet requirement: "The system must generate reports."

            **Modification Suggestions**
            To meet the reporting requirement, add the following function to the existing code:

            ```python
            def generate_report(data):
                # Logic to generate the report
                report = create_report(data)
                save_report(report)
                return report
            ```

            Ensure your suggestions include specific changes or additions to the code, and do not request the user to create them.

            ### Self-Reflection:
            # 1. **Initial Generation**: 
            #     - Generate the initial response based on the provided file.
            # 2. **Critical Review**:
            #     - Review the initial response, identify strengths and weaknesses, and propose improvements.
            # 3. **Iteration**:
            #     - Refine the initial response based on the suggestions from the critical review.
            # 4. **Continuous Cycle**:
            #     - Repeat the review and refinement until a satisfactory response is achieved.

            Answer in Brazilian Portuguese.
            """,
        )
    ]
)


prompt_regras_negocios = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            **Conversation History**: {history}\n
            ---------------------------------------------------------------\n
            Answer in Portuguese Brazil.
            Your main objective is to receive a code, interpret it, and abstract the understanding of the business rules it implements.

            Definition of business rules: Business rules translate a business context into a product or service; they describe how the product is expected to behave: conditions, constraints, triggers, etc.

            Instructions:
            1. Start with the title "REGRAS DE NEGÓCIO", followed by the implemented rules.
            2. For each business rule, describe:
                - What it does (Objective - PES).
                - How it is implemented (Relevant Code Details - AR).
                - Necessary conditions for its execution (Criteria - Al).
                - Applied constraints (Constraints - R).
                - Triggers that activate the rule (Triggers).
            3. Be detailed and verbose, providing clear and complete explanations every time.

            Code: {code}

            User's Question: {user_question}

            ### Self-Reflection:
            # 1. **Initial Generation**: 
            #     - Generate the initial response based on the provided code.
            # 2. **Critical Review**:
            #     - Review the initial response, identify strengths and weaknesses, and suggest improvements.
            # 3. **Iteration**:
            #     - Refine the initial response based on the critical review suggestions.
            # 4. **Continuous Cycle**:
            #     - Repeat the review and refinement process until a satisfactory response is achieved.


            Answer in Portuguese Brazil.
            """,
        )
    ]
)

prompt_verifica_resposta = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            Check if the question is formatted exactly like the example, without markdown formatting such as **text** or anything else extra.
            
            If it is, return the question unchanged.
            If it is not, return the response formatted correctly without changing the content of the question.
            Resposta: {pergunta}

            Example:

            Começo Nós

            Início @@ A
            Processo @@ BA
            Decisão @@ C
            Processo @@ D
            Fim @@ E

            Fim Nós

            Começo Conexões

            A @@ BA
            BA @@ C
            C @@ D @@ [Condição verdadeira]
            C @@ BA @@ [Condição falsa]
            D @@ E

            Fim Conexões

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
               - Liste todas as funções do arquivo.
               - Liste as dependências externas necessárias para o correto funcionamento do arquivo.
            Formato do Resumo:
            - O resumo deve ser claro e conciso, organizado em seções distintas para cada aspecto abordado (propósito, funções, processos, lógica, dependências).
            - Certifique-se de que o resumo ajude na compreensão do arquivo, permitindo uma análise aprofundada de suas funcionalidades e integrações com outros componentes do projeto.
            Exemplo:
            Propósito:
            O arquivo `arquivo.cs` tem como objetivo principal realizar a manipulação de dados do usuário dentro do sistema XYZ.
            Funções:
            - `funcao_principal`: Responsável por processar dados de entrada e gerar saídas formatadas.
                - `Dependências`: 
                    - `funcao_auxiliar`: Para realizar operações de validação de dados.
            - `funcao_auxiliar`: Suporta `funcao_principal` ao realizar operações de validação de dados.
            Processos e Lógica:
            Este arquivo utiliza uma abordagem de processamento sequencial para validar e processar dados de entrada, aplicando regras específicas de negócios.
            Dependências:
            - `outro_arquivo.cs`: Importado para acesso a funções de utilidade.
            - `biblioteca_A`: Utilizada para manipulação de strings.
            Arquivo: {arquivo}

            ### Autorreflexão:
            # 1. **Geração Inicial**: 
            #     - Gere a resposta inicial com base no arquivo fornecido.
            # 2. **Revisão Crítica**:
            #     - Revise a resposta inicial, identifique pontos fortes e fracos, e proponha melhorias.
            # 3. **Iteração**:
            #     - Refine a resposta inicial com base nas sugestões da revisão crítica.
            # 4. **Ciclo Contínuo**:
            #     - Repita a revisão e refinamento até alcançar uma resposta satisfatória.
            """,
        )
    ]
)

prompt_normal = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            **Conversation History**: {history}\n
            ---------------------------------------------------------------\n
            Question: {user_question}
            """,
        )
    ]
)
