CREATE DATABASE IF NOT EXISTS gerencia_ti;
USE gerencia_ti;

CREATE TABLE users (
    id INT AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(25) UNIQUE NOT NULL,
    password VARCHAR(25) NOT NULL
);

CREATE TABLE chats (
    id INT AUTO_INCREMENT PRIMARY KEY,
    title VARCHAR(40) NOT NULL,
    user_id INT NOT NULL,
    FOREIGN KEY (user_id) REFERENCES users(id)
);

CREATE TABLE messages (
    id INT AUTO_INCREMENT PRIMARY KEY,
    text_usuario TEXT NOT NULL,
    text_servidor TEXT NOT NULL,
    chat_id INT NOT NULL,
    FOREIGN KEY (chat_id) REFERENCES chats(id)
);
