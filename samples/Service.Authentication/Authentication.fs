namespace Service

open System
open System.Text

/// Back-end for the Authentication service.
module Authentication =
    type Authenticate = { username:string; password:string }

    let isAuthorized command =
        async {
            return command.username = "jbloggs" && command.password = "letmein"
        }