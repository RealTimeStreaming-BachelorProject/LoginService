#!/bin/bash

PORT=${LOGINSERVICE_PORT:-5005}

dotnet /app/LoginService.dll --urls=http://0.0.0.0:"$PORT"