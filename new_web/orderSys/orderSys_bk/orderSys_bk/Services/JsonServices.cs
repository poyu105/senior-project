using System.Text.Json;
using System.Text.Json.Serialization;

namespace orderSys_bk.Services
{
    public class JsonServices
    {
        //物件轉JSON字串
        public static string ToJson(object obj)
        {
            return JsonSerializer.Serialize(obj, new JsonSerializerOptions
            {
                ReferenceHandler = ReferenceHandler.IgnoreCycles,
                WriteIndented = false
            });
        }

        //JSON轉int
        public static int ToInt(object val, int defaultValue = 0)
        {
            if (val == null) return defaultValue;

            if (val is JsonElement elem)
            {
                if (elem.ValueKind == JsonValueKind.Number)
                    return elem.GetInt32();
                if (elem.ValueKind == JsonValueKind.String && int.TryParse(elem.GetString(), out var parsed))
                    return parsed;
            }

            return Convert.ToInt32(val);
        }

        //JsonElement轉Net物件
        public static object ToNetObject(object obj)
        {
            if (obj is JsonElement elem)
            {
                switch (elem.ValueKind)
                {
                    case JsonValueKind.Number:
                        if (elem.TryGetInt32(out int i)) return i;
                        if (elem.TryGetInt64(out long l)) return l;
                        if (elem.TryGetDouble(out double d)) return d;
                        return elem.GetDecimal();

                    case JsonValueKind.String:
                        return elem.GetString();

                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        return elem.GetBoolean();

                    case JsonValueKind.Object:
                        return elem.EnumerateObject()
                            .ToDictionary(
                                prop => prop.Name,
                                prop => ToNetObject(prop.Value)
                            );

                    case JsonValueKind.Array:
                        return elem.EnumerateArray()
                            .Select(e => ToNetObject(e))
                            .ToList();

                    case JsonValueKind.Null:
                    case JsonValueKind.Undefined:
                        return null;

                    default:
                        return elem.ToString();
                }
            }

            // 不是 JsonElement 就直接回傳原物件
            return obj;
        }


        //JSON轉Dictionary
        public static Dictionary<string, object> ToDictionary(object obj)
        {
            if (obj is JsonElement elem && elem.ValueKind == JsonValueKind.Object)
            {
                return elem.EnumerateObject()
                    .ToDictionary(
                        prop => prop.Name,
                        prop => ToNetObject(prop.Value)
                    );
            }
            return obj as Dictionary<string, object>;
        }

        //JSON轉List<Dictionary<string, object>>
        public static List<Dictionary<string, object>> ToListOfDictionary(object obj)
        {
            if (obj is JsonElement elem && elem.ValueKind == JsonValueKind.Array)
            {
                return elem.EnumerateArray()
                    .Select(e => ToNetObject(e) as Dictionary<string, object>)
                    .Where(d => d != null)
                    .ToList();
            }
            else if (obj is List<object> objList)
            {
                return objList
                    .Select(o => o as Dictionary<string, object>)
                    .Where(d => d != null)
                    .ToList();
            }
            return null;
        }

    }
}
