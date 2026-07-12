# Define a imagem base oficial do Node.js
FROM node:22-alpine

# Define o diretório de trabalho dentro do contêiner
WORKDIR /app

# Copia o arquivo de dependências primeiro para aproveitar o cache
COPY package*.json ./

# Instala as dependências do projeto
RUN npm install

# Copia o restante do código-fonte para o contêiner
COPY . .

# Expõe a porta em que a aplicação vai rodar
EXPOSE 3000

# Cria e define um usuário sem privilégios de root por segurança
USER node

# Comando padrão para iniciar a aplicação
CMD ["npm", "start"]
