import bodyParser from "body-parser";
import express from "express";
import { dbConfig } from "./models";
import { AuthRoutes } from "./routes";

class App {
  public app: express.Application;

  constructor() {
    this.app = express();
    this.config();
    this.databaseSetup();
    this.routesSetup();
  }

  private config(): void {
    this.app.use(bodyParser.json());
    this.app.use(bodyParser.urlencoded({ extended: false }));
    this.app.use((req, res, next) => {
      res.header("Access-Control-Allow-Origin", "*");
      res.header(
        "Access-Control-Allow-Headers",
        "Origin, X-Requested-With, Content-Type, Accept, Authorization"
      );
      res.header("Access-Control-Allow-Methods", "GET,POST,PATCH,DELETE");
      next();
    });
  }

  private databaseSetup() {
    dbConfig
      .authenticate()
      .then(() => console.log("connected to db"))
      .catch((err) => {
        throw err;
      });
  }

  private routesSetup() {
    new AuthRoutes().routes(this.app);
  }
}

const PORT = process.env.PORT || 5005;

new App().app.listen(PORT, () => {
  console.log("Server is running on: " + PORT);
});
