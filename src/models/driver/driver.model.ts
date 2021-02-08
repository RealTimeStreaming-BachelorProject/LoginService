import { DataTypes, Sequelize } from 'sequelize';
import { DriverStatic } from './driver.typings';

export function DriverFactory (sequelize: Sequelize): DriverStatic {
    return <DriverStatic>sequelize.define("drivers", {
        id: {
            type: DataTypes.UUIDV4,
            primaryKey: true,
        },
        username: {
            type: DataTypes.STRING,
            allowNull: false,
        },
        passwordHash: {
            type: DataTypes.STRING,
            allowNull: false,
        },
        createdAt: {
            type: DataTypes.DATE,
            allowNull: false,
            defaultValue: DataTypes.NOW,
        },
        updatedAt: {
            type: DataTypes.DATE,
            allowNull: false,
            defaultValue: DataTypes.NOW,
        },
    });
}