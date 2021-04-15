CREATE KEYSPACE IF NOT EXISTS loginservice;

USE loginservice;

CREATE TABLE IF NOT EXISTS drivers (
    id UUID PRIMARY KEY,
    username VARCHAR,
    hashedPassword VARCHAR,
    passwordUpdateEmulation VARCHAR
) WITH transactions = { 'enabled' : true };

CREATE UNIQUE INDEX driverusername ON drivers (username);

