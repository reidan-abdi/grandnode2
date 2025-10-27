FROM mongo:latest

COPY init-mongo.js /docker-entrypoint-initdb.d/

ENV MONGO_INITDB_ROOT_USERNAME=root
ENV MONGO_INITDB_ROOT_PASSWORD=example
ENV MONGO_INITDB_DATABASE=grandnodedb2

# Открываем стандартный порт MongoDB
EXPOSE 27017
