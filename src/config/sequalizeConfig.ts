export default {
  database: process.env.DATABASE_NAME ?? "loginservice",
  username: process.env.DATABASE_USERNAME ?? "loginserviceuser",
  password: process.env.DATABASE_PASSWORD ?? "loginservicepasswd",
  params: {
    host: process.env.DATABASE_HOST ?? "localhost",
    dialect: process.env.DATABASE_DIALECT ?? "postgres",
    operatorsAliases: false,
  },
};
