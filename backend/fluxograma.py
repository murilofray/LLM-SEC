import graphviz
def processar_resposta(resposta_llm):
    nos = []
    conexoes = []
    tipo = None

    for linha in resposta_llm.split('\n'):
        if 'Começo Nós' in linha:
            tipo = 'no'
        elif 'Fim Nós' in linha:
            tipo = None
        elif 'Começo Conexões' in linha:
            tipo = 'conexao'
        elif 'Fim Conexões' in linha:
            tipo = None
        elif tipo == 'no' and linha.strip() != '':
            partes = linha.strip().split()
            if len(partes) > 1:
                no_id = partes[-1][1:-1]  # extrai o ID do nó entre parênteses
                descricao = ' '.join(partes[:-1])
            else:
                no_id = None
                descricao = partes[0]
            nos.append((no_id, descricao))
        elif tipo == 'conexao' and linha.strip() != '':
            partes = linha.strip().split()
            if len(partes) > 2:
                origem = partes[0]
                destino = partes[2]
                if '[' in linha and ']' in linha:
                    condicao = linha[linha.find('[') + 1:linha.find(']')]
                else:
                    condicao = None
                conexoes.append((origem, destino, condicao))
            elif len(partes) == 2:
                origem = partes[0]
                destino = partes[1]
                conexoes.append((origem, destino))

    return nos, conexoes


def criar_fluxograma(nome_arquivo, resposta):
    dot = graphviz.Digraph(graph_attr={'rankdir': 'LR'})
    nos, conexoes = processar_resposta(resposta)
    # Adiciona os nós ao diagrama com estilos personalizados
    for no_id, descricao in nos:
        print("No: ", no_id, " Descricão: ", descricao)
        if descricao:
            dot.node(no_id, descricao, shape='box', style='filled', fillcolor='lightblue', fontname='Arial', fontsize='10')
        else:
            dot.node(no_id, shape='box', style='filled', fillcolor='lightblue', fontname='Arial', fontsize='10')

    # Adiciona as conexões ao diagrama com estilos personalizados
    for conexao in conexoes:
        origem = conexao[0][-1]  # Obtém o último caractere da origem (que é o ID do nó)
        destino = conexao[1][0]  # Obtém o primeiro caractere do destino (que é o ID do nó)
        if len(conexao) == 3:
            condicao = conexao[2]
            dot.edge(origem, destino, label=condicao, color='gray', fontname='Arial', fontsize='8')
        else:
            dot.edge(origem, destino, color='gray', fontname='Arial', fontsize='8')

    # Adiciona a legenda no canto do gráfico com HTML formatado corretamente
    legenda = '''
    Legenda:
    Nós do Fluxograma: Nome da Função 
                      [Saída Gerada Caso Tenha] - 
    Conexões - Condição para ir para o próximo nó
    '''
    dot.attr(label=legenda, labelloc="bottom", labeljust="left")
    # Renderiza o gráfico
    dot.render(nome_arquivo, view=True)
