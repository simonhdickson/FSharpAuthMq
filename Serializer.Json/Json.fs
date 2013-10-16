namespace Serializer
open Microsoft.FSharp.Reflection
open Newtonsoft.Json
open Newtonsoft.Json.Linq

module Json =
    let serialize (converters:JsonConverter[]) v = 
        JsonConvert.SerializeObject(v, Formatting.Indented, converters)

    let deserialize (converters:JsonConverter[]) v = 
        JsonConvert.DeserializeObject<'t>(v, converters)

    type UnionConverter<'u>() =
        inherit JsonConverter()
        let union = typeof<'u>
        let cases  = union |> FSharpType.GetUnionCases
        let names  = 
            cases |> Array.map (fun c -> let nm = union.Name + "+" + c.Name
                                         union.FullName.Replace(union.Name,nm))

        let deserialize (value, valueType) =
            match valueType with
            | t when t = typeof<string> -> value :> obj
            | _ -> JsonConvert.DeserializeObject(value, valueType)

        override __.WriteJson(writer,value,serializer) =
            match value with
            | null -> nullArg "value"
            | data -> 
                let (caseInfo, values) = FSharpValue.GetUnionFields(data, union)
                writer.WriteStartObject()
                writer.WritePropertyName(caseInfo.Name)
                writer.WriteStartArray()
                values |> Seq.iter writer.WriteValue
                writer.WriteEndArray()
                writer.WriteEndObject()

        override __.ReadJson(reader,_,_,serializer) =
            let jObj =
                serializer.Deserialize reader
                |> JObject.FromObject
                |> (fun i -> i.First :?> JProperty)
            let caseInfo = cases |> Seq.find (fun i -> i.Name = jObj.Name.Replace("_",""))
            let caseTypes = caseInfo.GetFields() |> Seq.map (fun i -> i.PropertyType)
            let args =
                jObj.Value
                |> Seq.map2 (fun i j -> (j.Value<string>(), i)) caseTypes
                |> Seq.map deserialize
                |> Seq.toArray
            FSharpValue.MakeUnion(caseInfo,args)

        override __.CanConvert(vType) = 
            (vType = union) || 
            (names |> Array.exists (fun n -> n = vType.FullName.Replace("_","")))