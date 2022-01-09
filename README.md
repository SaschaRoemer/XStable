## An eXtra Small table class that brings some SpecFlow.Assist Table features to xUnit (or a different .net test library of your choice)

Features:

* Create a table form a formatted string
* Compare (assert) equality of two tables

### Create table
```
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
        @"| StringProperty | IntProperty | DoubleProperty | EnumProperty       |
          | 1              | 2           | 3              | RemoveEmptyEntries |
          | 4              | 5           | 6              | None               |");
    }
}
```

### Compare table
```
public class XStableTest
{
    private DataObjectContextMock _mock = new();

    public void XStableTestEqual()
    {
        _mock.Table.Equal(
        @"| StringProperty | IntProperty | DoubleProperty | EnumProperty       |
          | 4              | 5           | 6              | None               |",
        _mock.Data);
    }
}

-->

System.ArgumentException :
   | StringProperty | IntProperty | DoubleProperty | EnumProperty       |
   | 4              | 5           | 6              | None               |
+  | 1              | 2           | 3              | RemoveEmptyEntries |
```

## Performance considerations

SpecFlow.Assist has a nice feature to find properties having a similar name as the column names of a table - in case no exact match exists. If used inappropriately it can slow down a test and may have an impact on the test suit.

I decided to implement only the features that do not have an extra cost on the execution time and drop anything else. There is still a good amount of reflection used, so you should expact an impact compared to the usabe of object instances directly.
