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

    [<Struct>]
    type UserStruct =
        member __.UserDefinition : string = "Fun!"

    let getDefaultUserRecord () =
        { UserDescription = "Cherry" }

    let useUserRecord userRecord :UserRecord = userRecord

    let getDefaultUserStruct () = UserStruct()

    let useUserStruct userStruct :UserStruct = userStruct

    module ArrayTest =
        let getDefaultUserRecordArray count =
            [| for i in 0 .. (count - 1) -> getDefaultUserRecord () |]

        let getDefaultUserStructArray count = 
            [| for i in 0 .. (count - 1) -> getDefaultUserStruct () |]
    
