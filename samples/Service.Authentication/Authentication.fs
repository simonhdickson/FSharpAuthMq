namespace Service

open System
open System.Text

/// Back-end for the Authentication service.
module Authentication =
    type Authenticate = { username:string; password:string }

    let isAuthorized command =
        async {
            if command.username = "Bob" && command.password = "letmein" then
                return true
            else
                return false
        }