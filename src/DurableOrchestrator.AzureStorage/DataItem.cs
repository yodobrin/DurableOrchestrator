using Parquet.Data;
using Parquet.Schema;
using System.Text.Json.Serialization;
// using System;
// using System.Collections.Generic;
// using System.Linq;

namespace DurableOrchestrator.AzureStorage;

public class DataItem
{
    [JsonPropertyName("dataModelName")]
    public string DataModelName { get; set; } = string.Empty;

    [JsonPropertyName("operation")]
    public string Operation { get; set; } = string.Empty;

    [JsonPropertyName("factory")]
    public int Factory { get; set; }

    [JsonPropertyName("lineId")]
    public int LineId { get; set; }

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("feature1")]
    public int Feature1 { get; set; }

    [JsonPropertyName("dim")]
    public int Dim { get; set; }

    [JsonPropertyName("yield")]
    public int Yield { get; set; }
    public static ParquetSchema GetSchema()
    {
        return new ParquetSchema(
            new DataField<string>("DataModelName"),
            new DataField<string>("Operation"),
            new DataField<int>("Factory"),
            new DataField<int>("LineId"),
            new DataField<DateTime>("Date"),
            new DataField<int>("Feature1"),
            new DataField<int>("Dim"),
            new DataField<int>("Yield"));
    }
    public static List<DataColumn> ConvertToDataColumns(List<DataItem> items)
    {
        // Parquet.Schema
        var schema = GetSchema();
        List<DataColumn> columns = new List<DataColumn>
        {
            new DataColumn(schema.DataFields[0], items.Select(x => x.DataModelName).ToArray()),
            new DataColumn(schema.DataFields[1], items.Select(x => x.Operation).ToArray()),
            new DataColumn(schema.DataFields[2], items.Select(x => x.Factory).ToArray()),
            new DataColumn(schema.DataFields[3], items.Select(x => x.LineId).ToArray()),
            new DataColumn(schema.DataFields[4], items.Select(x => x.Date).ToArray()),
            new DataColumn(schema.DataFields[5], items.Select(x => x.Feature1).ToArray()),
            new DataColumn(schema.DataFields[6], items.Select(x => x.Dim).ToArray()),
            new DataColumn(schema.DataFields[7], items.Select(x => x.Yield).ToArray())
        };

        return columns;
    }
}
