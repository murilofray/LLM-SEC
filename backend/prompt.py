from langchain_core.prompts import ChatPromptTemplate, MessagesPlaceholder

inicial_prompt = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            History memory: {historico}
            ---------------------------------
            Your task is to identify which of the prompts the user's question will use.
            There are four prompts: prompt_arquivo, prompt_fluxograma, prompt_regras, and prompt_analise_codigo. You can use one of them or none.
            
            - **Code flowchart**: use `prompt_fluxograma` when the question refers to the structure or flow of the code. Examples: "What is the flow of the data in this process?"
            
            - **Business rules of the code**: use `prompt_regras` when the question refers to the business rules implemented in the code. Examples: "What business rules are enforced in this function?" or "Describe the rules implemented in this code."
            
            - **Business requirements**: use `prompt_arquivo` when the question refers to requirements in a document and/or generating requirements. Examples: "What are the requirements for this project?" or "Generate the business requirements from this document."
            
            - **Code modification based on requirements**: use `prompt_analise_codigo` when the question refers to the need for code changes to meet business requirements. Examples: "What changes are needed in the code to meet these requirements?" or "Modify the code to fulfill the new business requirements."
            
            - Only use the prompts when generating something like a flowchart, business rules, requirement analysis, or code modifications. For questions that do not request generating anything, use "nenhum".
            
            Return only the name of the chosen prompt. If none apply, return only "nenhum".
            Pergunta: {user_question}
            Answer in Portuguese Brazil.
            """
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
            Objective: Identify and list the implementation demands for the process described in the requirements document.

            Instructions:

            1. Thoroughly read the document: Carefully review the document, paying attention to all details and sections.

            2. Identify key sections: Locate sections of the document that specify implementation requirements. These may include:

                - Functional descriptions
                - Technical requirements
                - Integrations with external systems
                - Data migration procedures
            
            3. Extract relevant information: For each identified section, extract information about:

                - Specific functionalities that need to be developed or configured.
                - Technologies or tools that will be used.
                - Steps required for implementation.
                - Dependencies and integrations with other systems.
                - Timelines and schedules.
                - Necessary human and material resources.
                - Acceptance criteria and success metrics.
            
            4. List the demands: Organize the demands into a clear and structured list. Each item in the list should include:

                - A brief description of the demand.
                - The origin of the demand in the document (e.g., section or paragraph).
                - Specific requirements and implementation steps.
                - Any dependencies or prerequisites.
                - Required resources.
                - Acceptance criteria.
           
            Format of the List:

            Demanda 1:\n

            Description: [Brief description of the demand]\n
            Origin: [Section/Paragraph]\n
            Requirements: [Details of the requirements]\n
            Implementation Steps: [Necessary steps]\n
            Dependencies: [Dependent systems or processes]\n
            Required Resources: [Human/material resources]\n
            Acceptance Criteria: [Success metrics]\n

            Demanda 2:\n

            Description: [Brief description of the demand]\n
            Origin: [Section/Paragraph]\n
            Requirements: [Details of the requirements]\n
            Implementation Steps: [Necessary steps]\n
            Dependencies: [Dependent systems or processes]\n
            Required Resources: [Human/material resources]\n
            Acceptance Criteria: [Success metrics]\n
            Practical Example:\n

            Imagine the document includes a requirement to integrate a new student management module with an existing database system. The demand could be described as follows:

            Demanda 1:\n
            Description: Integration of the student management module with the existing database.\n
            Origin: Section 3.2, Paragraph 4\n
            Requirements: The module must read and write data to the existing SQL database.\n
            Implementation Steps: Develop integration APIs, conduct connection tests, validate data.\n
            Dependencies: SQL database system.\n
            Required Resources: Developers with SQL and API experience, testing environment.\n
            Acceptance Criteria: Stable connectivity, accurate data read/write operations.\n
            Finalization:

            After listing all the demands, review the list to ensure completeness and clarity. This list will serve as the foundation for planning and executing the process implementation.
            Requirements Document: {docs}
            Question: {user_question}

            Answer in Portuguese Brazil.
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
            Your goal is to analyze the user's question and the provided code, identify the main execution flow and the generated data, and structure this information clearly and consistently to be used in generating a textual flowchart later.

            Follow the instructions below to perform the analysis:

            1. Identify the `main` function in the code and start the analysis from there. If the user's question specifies a different function, start the analysis from that function.
            2. List all processes or steps that occur within that function and the functions it calls sequentially.
            3. Include any important conditions or branches.
            4. Identify and list any files or data generated during code execution.
            5. Try not to create too many processes or steps. If you find an unnecessary process or step, ignore it or add it as an action in a previous process or step.

            Structure your answer as follows:

            **Objetivo principal:**
            - Describe the main objective of the code in a clear sentence.

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
            Do not use the example in the answer, it is just an example
            Examples:

            **Example 1**

            **Question:**
            How does the student grade calculation process work?

            **Code:**
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

            **Answer:**

            **Main Objective:**
            - The code calculates the final grades of students based on their scores and saves the results in a file.

            **Main Processes/Steps:**
            1. Student Data Retrieval
                - Loads the list of students and their scores.
            2. Grade Calculation
                - Calculates the total scores for each student.
            3. Final Grade Assignment
                - Assigns a final grade (A, B, C, or F) based on the total scores.
            4. Saving Results
                - Saves the final grades in a text file.

            **Important Conditions:**
            - If the total scores are greater than or equal to 90, the student receives an A grade.
            - If the total scores are greater than or equal to 80, the student receives a B grade.
            - If the total scores are greater than or equal to 70, the student receives a C grade.
            - If the total scores are less than 70, the student receives an F grade.

            **Generated Files or Data:**
            - `final_grades.txt` text file containing the final grades of students.
            Do not use the example in the answer, it is just an example
            Now, analyze the following code and provide the high-level description strictly following the format above, but adapt as necessary to accurately reflect the code's operation:

            **Question:**
            {user_question}

            **Code:**
            {codigo}
            Answer in Portuguese Brazil.
            """
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

            {descricao_fluxo}

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
            """
        )
    ]
)

alteracao_prompt = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            ### Contexto
            Você está encarregado de analisar o código-fonte para garantir que ele atenda aos requisitos de negócios especificados. Seu objetivo é identificar quaisquer lacunas entre os requisitos de negócios e o código implementado. Use as informações fornecidas para sugerir as modificações necessárias para alinhar o código com todos os requisitos de negócios.

            ### Pergunta do Usuário
            {pergunta}

            ### Requisitos de Negócios
            {requisitos}

            ### Código-Fonte Atual
            {codigo}

            ### Banco de Dados
            {database}

            ### Consultas Existentes
            {query}

            ### Tarefa
            Com base nos requisitos de negócios fornecidos e no código-fonte, identifique quais requisitos não estão sendo atendidos. Em seguida, sugira modificações no código-fonte para atender a todos os requisitos de negócios.

            ### Saída Esperada
            1. **Análise**: Identifique os requisitos de negócios que não estão sendo atendidos pelo código-fonte.
            2. **Sugestões de Modificação**: Forneça modificações específicas no código-fonte que abordem as lacunas identificadas.

            ### Exemplo de Saída

            **Análise**
            Requisito não atendido: "O sistema deve gerar relatórios."

            **Sugestões de Modificação**
            Para atender ao requisito de geração de relatórios, adicione a seguinte função ao código existente:

            ```python
            def generate_report(data):
                # Lógica para gerar o relatório
                report = create_report(data)
                save_report(report)
                return report
            ```

            Certifique-se de que suas sugestões incluam mudanças ou adições específicas ao código e não solicitem ao usuário que as crie.

            Responda em português do Brasil.
            """
        )
    ]
)

prompt_regras_negocios = ChatPromptTemplate.from_messages(
    [
        (
            "user",
            """
            Memory History: {history}
            ---------------------------------
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

            Code: {codigo}

            User's Question: {user_question}
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

            """
        )
    ]
)

prompt_normal = ChatPromptTemplate.from_messages(
    [
        ("system", "You are a helpful assistant. Answer in Portuguese Brazil."),
        MessagesPlaceholder(variable_name="history"),
        ("user", "{input}, Answer in Portuguese Brazil."),
    ]
)
