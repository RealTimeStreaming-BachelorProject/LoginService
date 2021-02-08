import sequelizeConfig from "../config/sequalizeConfig";
import sequelize from 'sequelize';
import { DriverFactory } from "./driver/driver.model";

export const dbConfig = new sequelize.Sequelize(sequelizeConfig)

export const Driver = DriverFactory(dbConfig);