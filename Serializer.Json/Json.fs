namespace Serializer

open Newtonsoft.Json

module Json =
    let serialize (converters:JsonConverter[]) v = 
        JsonConvert.SerializeObject(v, Formatting.Indented, converters)

    let deserialize (converters:JsonConverter[]) v = 
        JsonConvert.DeserializeObject<'t>(v, converters)