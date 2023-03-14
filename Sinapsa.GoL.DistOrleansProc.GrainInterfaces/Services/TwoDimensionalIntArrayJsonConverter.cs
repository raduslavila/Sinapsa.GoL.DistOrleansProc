//using Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Models;
//using System.Text.Json;
//using System.Text.Json.Serialization;

//namespace Sinapsa.GoL.DistOrleansProc.GrainInterfaces.Services
//{
//    public class TwoDimensionalCellArrayJsonConverter : JsonConverter<Cell[,]>
//    {
//        public override Cell[,]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//        {
//            using var jsonDoc = JsonDocument.ParseValue(ref reader);

//            var rowLength = jsonDoc.RootElement.GetArrayLength();
//            var columnLength = jsonDoc.RootElement.EnumerateArray().First().GetArrayLength();

//            Cell[,] grid = new Cell[rowLength, columnLength];

//            int row = 0;
//            foreach (var array in jsonDoc.RootElement.EnumerateArray())
//            {
//                int column = 0;
//                foreach (var number in array.EnumerateArray())
//                {
//                    grid[row, column] = number.Deserialize<Cell>();
//                    column++;
//                }
//                row++;
//            }
//            return grid;
//        }
//        public override void Write(Utf8JsonWriter writer, Cell[,] value, JsonSerializerOptions options)
//        {
//            throw new NotImplementedException();
//            //writer.WriteStartArray();
//            //for (int i = 0; i < value.GetLength(0); i++)
//            //{
//            //    writer.WriteStartArray();
//            //    for (int j = 0; j < value.GetLength(1); j++)
//            //    {
//            //        writer.WriteStartObject();

//            //        writer.Write

//            //        writer.WriteEndObject();

//            //        writer.WriteNumberValue(value[i, j]);
//            //    }
//            //    writer.WriteEndArray();
//            //}
//            //writer.WriteEndArray();
//        }
//    }
//}
