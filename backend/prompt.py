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
            
            - **Code flowchart**: use `prompt_fluxograma` when the question refers to the structure or flow of the code. Examples: "How does this function work?" or "What is the flow of the data in this process?"
            
            - **Business rules of the code**: use `prompt_regras` when the question refers to the business rules implemented in the code. Examples: "What business rules are enforced in this function?" or "Describe the rules implemented in this code."
            
            - **Business requirements**: use `prompt_arquivo` when the question refers to requirements in a document and/or generating requirements. Examples: "What are the requirements for this project?" or "Generate the business requirements from this document."
            
            - **Code modification based on requirements**: use `prompt_analise_codigo` when the question refers to the need for code changes to meet business requirements. Examples: "What changes are needed in the code to meet these requirements?" or "Modify the code to fulfill the new business requirements."
            
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
            Your main task is to receive a document and, from it, generate the business requirements.
            If the question is about requirements, use the business requirements structure.
            Also, answer questions about the document's content that are not related to business requirements.
            For example: "What is the theme of the document?" In this case, you do not need to use the business requirements structure.
            ITINERÁRIOS FORMATIVOS are the set of disciplines, projects, workshops, study groups, among other work situations, that students can choose in high school.

            Consult only the context content to answer the question and generate business requirements.

            Business Requirements Structure:
            Use the following structure (add, remove or rename topics as needed):

            ##Requisitos de Negócios

            ##1° - [NAME OF TOPIC]\n
            [Generated business requirements]

            ##2° - [NAME OF TOPIC]\n
            [Generated business requirements

            ##3° - [NAME OF TOPIC]\n
            [Generated business requirements]

            ...

            Responda a seguinte pergunta: {user_question}
            Usando o seguinte contexto: {docs}
            Answer in Portuguese Brazil.

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
            ### Context
            You are tasked with analyzing the source code to ensure it meets specified business requirements. Your goal is to identify any gaps between the business requirements and the implemented code. Use the provided information to suggest necessary modifications to align the code with all business requirements.

            ### User's Question
            {pergunta}

            ### Business Requirements
            {requisitos}

            ### Business rules extracted from source code
            {regras}

            ### Current Source Code
            {codigo}

            ### Task
            Based on the provided business requirements and the source code, identify which requirements are not being met. Then, suggest modifications to the source code to fulfill all business requirements.

            ### Expected Output
            1. **Analysis**: Identify the business requirements that are not currently met by the source code.
            2. **Modification Suggestions**: Provide specific modifications to the source code that address the identified gaps.

            ### Example Output

            **Analysis**
            Unmet requirement: "The system must generate reports."

            **Modification Suggestions**
            To meet the requirement of generating reports, add the following function to the existing codebase:

            ```python
            def generate_report(data):
                # Logic to generate report
                report = create_report(data)
                save_report(report)
                return report
            ```

            Ensure your suggestions include specific code changes or additions and do not request the user to create them.

            Answer in Portuguese Brazil.
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
