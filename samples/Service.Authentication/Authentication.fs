namespace Service

open System
open System.Text

/// Back-end for the Authentication service.
module Authentication =
    type Authenticate = { username:string; password:string }

    type Revoke = { username:string }

    let isAuthorized (command:Authenticate) =
        async {
            return command.username = "jbloggs" && command.password = "letmein"
        }

    let revoke command =
        async {
            return command.username = "jbloggs"
        }