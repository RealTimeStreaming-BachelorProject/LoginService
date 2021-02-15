#!/bin/bash

export LOGINSERVICE_POSTGRES_CONNECTION_STRING="Host=localhost;Port=5432;Database=loginservice-database;Username=loginservice;Password=Loginservice_database_password1"
export LOGINSERVICE_JWT_ISSUER=https://localhost:5005
export LOGINSERVICE_JWT_KEY="developmentjwtkey"

# If the docker container already exists it will not be created again
docker run -d --name devdatabaseloginservice \
    --restart=always -p 5432:5432 \
    -e POSTGRES_USER=loginservice \
    -e POSTGRES_PASSWORD=Loginservice_database_password1 \
    -e POSTGRES_DB=loginservice-database \
    postgres

dotnet run --watch