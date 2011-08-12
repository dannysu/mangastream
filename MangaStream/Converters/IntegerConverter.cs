using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Newtonsoft.Json;

namespace MangaStream
{
    public class IntegerConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return (objectType == typeof(int) || objectType == typeof(Int32));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                throw new Exception(string.Format("Cannot convert null value to {0}.", objectType));
            }

            if (reader.TokenType != JsonToken.String)
            {
                throw new Exception(string.Format("Unexpected token parsing date. Expected String, got {0}.", reader.TokenType));
            }

            string str = reader.Value.ToString();
            return int.Parse(str);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            string str;
            if (value is int || value is Int32)
            {
                str = (int)value + "";
            }
            else
            {
                throw new Exception(string.Format("Unexpected object type. Expected int."));
            }
            writer.WriteValue(str);
        }
    }
}
