global using System.Collections.Generic;
global using System;
global using Xunit;
global using XStable;

namespace XStable.Test;

public class XStableTest
{
    private DataObjectContextMock _mock = new();

    [Fact]
    public void XStableTestEqual()
    {
        _mock.Table.Equal(
        @"| StringProperty | IntProperty | DoubleProperty | EnumProperty       | GuidProperty                           |
          | 1              | 2           | 3              | RemoveEmptyEntries | {422927f9-db0b-4bec-803f-476be86846db} |
          | 4              | 5           | 6              | None               |                                        |",
        _mock.Data);
    }

    [Fact]
    public void XStableTestEqual_OneAdditionalLine()
    {
        try
        {
            _mock.Table.Equal(
            @"| StringProperty | IntProperty | DoubleProperty | EnumProperty       | GuidProperty                           |
              | 4              | 5           | 6              | None               |                                        |",
            _mock.Data);
        }
        catch(Exception ex)
        {
            Assert.True(ex.Message.Contains(" +  | 1"), "Additional line.");
            Assert.True(ex.Message.Contains("    | 4"), "Existing line.");
            return;
        }

        Assert.True(false);
    }

    [Fact]
    public void XStableTestEqual_OneMissingLine()
    {
        try
        {
            _mock.Table.Equal(
        @"| StringProperty | IntProperty | DoubleProperty | EnumProperty       | GuidProperty                           |
          | 1              | 2           | 3              | RemoveEmptyEntries | {422927f9-db0b-4bec-803f-476be86846db} |
          | 4              | 5           | 6              | None               |                                        |
          | 7              |             |                | None               |                                        |",
            _mock.Data);
        }
        catch(Exception ex)
        {
            Assert.True(ex.Message.Contains(" -  | 7"), "Missing line.");
            return;
        }

        Assert.True(false);
    }
}