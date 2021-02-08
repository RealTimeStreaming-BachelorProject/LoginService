import { BuildOptions, Model } from "sequelize";

export interface DriverAttributes {
    id?: string,
    username: string,
    passwordHash: string,
    createdAt?: Date,
    updatedAt?: Date
}

export interface DriverModel extends Model<DriverAttributes>, DriverAttributes {}

export class Driver extends Model<DriverAttributes, DriverAttributes> {}

export type DriverStatic = typeof Model & {
   new (values?: object, options?: BuildOptions): DriverModel;
};