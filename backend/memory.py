class HistoryManager:
    def __init__(self):
        self.history = []

    def update_history(self, user_message, ai_response):
        # Adiciona a nova interação ao histórico
        self.history.append(("human", user_message))
        self.history.append(("ai", ai_response))

        # Mantém apenas as últimas seis interações
        if len(self.history) > 6:
            self.history = self.history[-6:]

    def format_history_for_prompt(self):
        history_str = ""
        for source, message in self.history:
            if source == "human":
                history_str += f"User: {message}\n"
            elif source == "ai":
                history_str += f"Assistant: {message}\n"
        return history_str

    def clear_history(self):
        self.history = []
