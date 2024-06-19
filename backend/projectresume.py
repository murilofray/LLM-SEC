# import os
# from backend.llm import *

# def processar_e_resumir_arquivos(user_folder):
#     """
#     Função para processar e resumir arquivos em cada pasta do diretório do usuário.
    
#     Args:
#     - user_folder (str): Caminho para o diretório do usuário contendo subpastas com arquivos.
    
#     Returns:
#     - dict: Dicionário com os caminhos dos arquivos organizados por pasta.
#     """
#     # Dicionário para armazenar os arquivos por pasta
#     arquivos_por_pasta = {}

#     # Verifica se o diretório do usuário existe
#     if not os.path.exists(user_folder):
#         raise ValueError(f"Diretório do usuário não encontrado: {user_folder}")

#     # Inicializa a chave para a pasta principal
#     arquivos_por_pasta["principal"] = []

#     # Itera sobre todas as pastas e arquivos dentro do diretório do usuário
#     for root, dirs, files in os.walk(user_folder):
#         # Adiciona arquivos da pasta principal
#         if root == user_folder:
#             for file_name in files:
#                 file_path = os.path.join(root, file_name)
#                 if os.path.isfile(file_path):
#                     arquivos_por_pasta["principal"].append(file_path)
        
#         # Adiciona arquivos das subpastas
#         for dir_name in dirs:
#             dir_path = os.path.join(root, dir_name)
#             arquivos_por_pasta[dir_name] = []
#             for file_name in os.listdir(dir_path):
#                 file_path = os.path.join(dir_path, file_name)
#                 if os.path.isfile(file_path):
#                     arquivos_por_pasta[dir_name].append(file_path)
#     # String para armazenar o resumo concatenado
#     resumo_concatenado = ""

#     # Adiciona cabeçalho com todos os diretórios e arquivos
#     cabecalho = "Diretórios e arquivos encontrados:\n"
#     for pasta, arquivos in arquivos_por_pasta.items():
#         cabecalho += f"\nPasta: {pasta}\n"
#         for arquivo in arquivos:
#             cabecalho += f"  - {arquivo}\n"
#     cabecalho += "\n\n"

#     # Adiciona o cabeçalho ao resumo concatenado
#     resumo_concatenado += cabecalho
#     regras = ""

#     # Processa e resume cada arquivo
#     for pasta, arquivos in arquivos_por_pasta.items():
#         for arquivo in arquivos:
#             try:
#                 with open(arquivo, 'r', encoding='utf-8') as f:
#                     conteudo = f.read()
#                     resposta = agent_resume_project.invoke({"arquivo": conteudo})
#                     resumo_concatenado += resposta['answer'] + "\n-----------------------------------------------------------------------------------\n"
#                     answer_fluxograma = agent_fluxograma3.invoke({"codigo": conteudo, "resumo": resposta['answer']})
#                     answer_fluxograma = answer_fluxograma['answer']
#                     print("Pergunta")
#                     answer_fluxograma = formata_resposta.invoke({"pergunta": answer_fluxograma})
#                     answer_fluxograma = answer_fluxograma['answer']
#                     print("Entrou no fluxograma")
#                     criar_fluxograma("fluxograma" + datetime.now().strftime("%Y%m%d_%H%M%S"),answer_fluxograma)
#                     answer_regras = agent_regras_negocios.invoke({"codigo": conteudo})
#                     answer_regras = answer_regras['answer']
#                     regras += answer_regras + "\n-----------------------------------------------------------------------------------\n"
#                     print("continua")
#             except Exception as e:
#                 print(f"Erro ao processar o arquivo {arquivo}: {str(e)}")
#     try:
#         output_txt_path = os.path.join(user_folder, 'resumo_project.txt')
#         output_regras_path = os.path.join(user_folder, 'resumo_regras.txt')
#         salvar_resumo_em_arquivo(resumo_concatenado,output_txt_path)
#         salvar_resumo_em_arquivo(regras,output_regras_path)
#         answer = agent_fluxograma2.invoke({"resumo": resumo_concatenado})
#         answer = answer['answer']
#         answer = formata_resposta.invoke({"pergunta": answer})
#         answer = answer['answer']
#         nome_fluxograma = "fluxograma" + datetime.now().strftime("%Y%m%d_%H%M%S")
#         criar_fluxograma(nome_fluxograma,answer)
#     except Exception as e:
#         print(f"Erro ao salvar o resumo em um arquivo: {str(e)}")
#         # Aqui você pode decidir o que fazer em caso de erro, como pular para o último arquivo ou registrar o erro
#     return resumo_concatenado



# def salvar_resumo_em_arquivo(resumo_texto, output_path):
#     """
#     Função para salvar um resumo em um arquivo de texto.

#     Args:
#     - resumo_texto (str): Texto concatenado de todos os resumos.
#     - output_path (str): Caminho onde o arquivo de texto será salvo.
#     """
#     print(f"Salvando resumo em: {output_path}")
#     with open(output_path, 'w', encoding='utf-8') as file:
#         file.write(resumo_texto)
#     print(f"Arquivo de resumo salvo em: {output_path}")