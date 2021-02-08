
import { Application } from "express";
import { AuthController } from "../controllers/index";

export default class AuthRoutes {
    public authController: AuthController = new AuthController();

    public routes(app: Application) {
        app.route("/register")
            .post(this.authController.register)
        app.route("/login")
            .post(this.authController.login)
        app.route("verifytoken")
            .post(this.authController.verifyToken)
    }
}