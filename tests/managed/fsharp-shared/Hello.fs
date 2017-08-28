namespace managed

type Hello() = 
    member this.World = "Hello from F#"

module FSharp =

    module NestedModuleTest =
        let nestedFunction () = 
            "Hello from a nested F# module"
        
        let nestedConstant = "Hello from a nested F# module"
    
    type UserRecord = {
        UserDescription : string
    }

    let getDefaultUserRecord () =
        { UserDescription = "Cherry" }
    
