namespace XStable.Test;

internal class DataObjectContextMock
{
    public Table<DataObject> Table =
        new Table<DataObject>()
        .AddParser("GuidProperty", e => Guid.Parse(e));

    public List<DataObject> Data { get; set; }

    public DataObjectContextMock()
    {
        Data = 
        Table
        .Parse(
        @"| StringProperty | IntProperty | DoubleProperty | EnumProperty       | GuidProperty                           |
          | 1              | 2           | 3              | RemoveEmptyEntries | {422927f9-db0b-4bec-803f-476be86846db} |
          | 4              | 5           | 6              | None               |                                        |");
    }
}