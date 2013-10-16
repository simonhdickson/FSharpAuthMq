namespace Serializer

open Microsoft.FSharp.Reflection
open Newtonsoft.Json
open Newtonsoft.Json.Linq

module Json =
    let serialize (converters:JsonConverter[]) v = 
        JsonConvert.SerializeObject(v, Formatting.Indented, converters)

    let deserialize (converters:JsonConverter[]) v = 
        JsonConvert.DeserializeObject<'t>(v, converters)