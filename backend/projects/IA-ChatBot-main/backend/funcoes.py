from backend.db import mysql

# Extrai o código SQL da mensagem
def extrair_sql(mensagem):
    partes_sql = mensagem.split("SQL121:")
    codigo_sql = " ".join(partes_sql)
    codigo_sql = codigo_sql.replace("```", "").strip()
    codigo_sql = codigo_sql.replace("SQL121:", "").strip()
    return mensagem

# Função para processar e executar um comando SQL a partir de uma mensagem
def processar_sql(mensagem):
    try:
        codigo_sql = extrair_sql(mensagem)
        # Encontra a posição da palavra "SELECT" para garantir que o comando SQL seja válido e que não seja um delete ou update
        if "SELECT" in codigo_sql:
            codigo_sql = codigo_sql[codigo_sql.find("SELECT"):]
            # Executa o comando SQL
            cursor = mysql.connection.cursor()
            cursor.execute(codigo_sql)
            resultado = cursor.fetchall()
            cursor.close()
            # Formata o resultado para uma string legível
            resultado_final = '\n'.join([str(linha) for linha in resultado])
            return resultado_final
        return "Comando SQL inválido."
    except Exception as e:
        return "Erro ao executar o código SQL: " + str(e)
