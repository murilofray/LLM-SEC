import graphviz
import os
def processar_resposta(resposta_llm):
    resposta_llm = resposta_llm.replace('*', '')
    resposta_llm = resposta_llm.replace('#', '')
    nos = []
    conexoes = []
    tipo = None

    # Dividindo a resposta em linhas
    linhas = resposta_llm.strip().split('\n')

    # Iterando pelas linhas
    i = 0
    while i < len(linhas):
        linha = linhas[i].strip()
        
        # Verifica o tipo de seção (nós ou conexões)
        if linha.startswith('Começo Nós'):
            tipo = 'no'
        elif linha.startswith('Começo Conexões'):
            tipo = 'conexao'
        elif linha.startswith('Fim Nós') or linha.startswith('Fim Conexões'):
            tipo = None
        else:
            # Processa nós
            if tipo == 'no' and linha != '':
                partes = linha.split('@@')
                if len(partes) >= 2:
                    no_id = partes[1].strip()
                    descricao = partes[0].strip()
                    if len(partes) == 3:
                        opcional = partes[2].strip()
                        nos.append((no_id, descricao, opcional))
                    else:
                        nos.append((no_id, descricao, None))
            
            # Processa conexões
            elif tipo == 'conexao' and linha != '':
                partes = linha.split('@@')
                if len(partes) >= 2:
                    origem = partes[0].strip()
                    destino = partes[1].strip()
                    if len(partes) == 3:
                        condicao = partes[2].strip()
                    else:
                        condicao = None
                    conexoes.append((origem, destino, condicao))

        i += 1

    return nos, conexoes

def criar_fluxograma(nome_arquivo, resposta):
    dot = graphviz.Digraph(graph_attr={'rankdir': 'TB'})
    nos, conexoes = processar_resposta(resposta)
    print(nos, conexoes)
    # Adiciona os nós ao diagrama com estilos personalizados
    for no in nos:
        if len(no) == 2:
            no_id, descricao = no
            opcional = None
        elif len(no) == 3:
            no_id, descricao, opcional = no
        else:
            raise ValueError(f"Nó inesperado encontrado: {no}")

        print("No: ", no_id, " Descrição: ", descricao, " Opcional: ", opcional)
        
        if descricao:
            dot.node(no_id, descricao, shape='box', style='filled', fillcolor='lightblue', fontname='Arial', fontsize='10')
        else:
            dot.node(no_id, shape='box', style='filled', fillcolor='lightblue', fontname='Arial', fontsize='10')

    # Adiciona as conexões ao diagrama com estilos personalizados
    for conexao in conexoes:
        origem = conexao[0]
        destino = conexao[1]
        if len(conexao) == 3:
            condicao = conexao[2]
            dot.edge(origem, destino, label=condicao, color='gray', fontname='Arial', fontsize='8')
        else:
            dot.edge(origem, destino, color='gray', fontname='Arial', fontsize='8')

    # Adiciona a legenda no canto do gráfico com HTML formatado corretamente
    legenda = '''
    Legenda:
    Nós do Fluxograma: Nome da Função 
    Conexões - Condição para ir para o próximo nó
    '''
    dot.attr(label=legenda, labelloc="bottom", labeljust="left")
    
    # Obter o diretório base onde o app.py está localizado
    try:
        base_directory = os.path.dirname(os.path.abspath(__file__))
        caminho_arquivo = os.path.join(base_directory, "fluxogramas", nome_arquivo)
    except NameError:
        caminho_arquivo = "/backend/fluxogramas"
    
    # Renderiza o gráfico
    dot.render(caminho_arquivo, view=False)
