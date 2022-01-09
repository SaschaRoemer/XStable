using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace XStable;

public class Table<T>
{
    private Dictionary<string, Func<string, object>> _parsers;

    public Table<T> AddParser(string cell, Func<string, object> value)
    {
        if (_parsers == null)
        {
            _parsers = new Dictionary<string, Func<string, object>>();
        }

        _parsers.Add(cell, value);

        return this;
    }

    public List<T> Parse(string table) =>
        ParseInternal(table, out var meta).ToList();

    protected List<T> ParseInternal(string table, out Meta[] meta)
    {
        var result = new List<T>();

        var rows =
            table
            .Split(System.Environment.NewLine)
            .Select(e => e.Trim())
            .ToArray();

        var header = rows[0];

        var headerCells = header.Split('|', StringSplitOptions.RemoveEmptyEntries);

        var targetType = typeof(T);

        meta = InitMeta(headerCells, targetType);

        var dataRows = rows.Skip(1).ToArray();

        foreach (var row in dataRows)
        {
            var dataCells = row.Split('|', StringSplitOptions.RemoveEmptyEntries);

            if (dataCells.Length != meta.Length) continue;

            var target = Activator.CreateInstance<T>();

            for (var i = 0; i < dataCells.Length; i++)
            {
                var cell = dataCells[i].Trim();

                if (string.IsNullOrEmpty(cell)) continue;

                var property = meta[i].Property;

                switch (property.PropertyType.FullName)
                {
                    case "System.String":
                        property.SetValue(target, cell);
                        break;
                    case "System.Boolean":
                        property.SetValue(target, bool.Parse(cell));
                        break;
                    case "System.Int32":
                        property.SetValue(target, int.Parse(cell));
                        break;
                    case "System.UInt32":
                        property.SetValue(target, uint.Parse(cell));
                        break;
                    case "System.Int64":
                        property.SetValue(target, long.Parse(cell));
                        break;
                    case "System.UInt64":
                        property.SetValue(target, ulong.Parse(cell));
                        break;
                    case "System.Float":
                        property.SetValue(target, float.Parse(cell));
                        break;
                    case "System.Double":
                        property.SetValue(target, double.Parse(cell));
                        break;
                    case "System.Decimal":
                        property.SetValue(target, decimal.Parse(cell));
                        break;
                    // TODO Add enum support.
                    default:
                        if (property.PropertyType.IsEnum)
                        {
                            property
                            .SetValue(
                                target,
                                Enum.Parse(property.PropertyType, cell));
                        }

                        else if (_parsers.TryGetValue(property.Name, out var parser))
                        {
                            property.SetValue(target, parser(cell));
                        }

                        else throw new InvalidOperationException($"Unsupported property type {property.PropertyType.FullName}");
                        break;
                }
            }

            result.Add(target);
        }

        return result;
    }

    private string[] Print(T[] obj, Meta[] meta)
    {
        if (obj == null) obj = new T[0];

        var result = new string[obj.Length + 1];
        var rows = new List<string[]>(obj.Length);
        var width = new int[meta.Length];

        rows.Add(meta.Select(e => e.Header).ToArray());

        foreach(var o in obj)
        {
            var c = new string[meta.Length];

            for (var i = 0; i < meta.Length; i++)
            c[i] = meta[i].Property.GetValue(o)?.ToString();

            rows.Add(c);
        }

        for (var i = 0; i < meta.Length; i++)
        width[i] = rows.Max(e => e[i]?.Length ?? 0);

        var index = 0;
        foreach(var row in rows)
        {
            var sb = new StringBuilder();

            for(var i = 0; i < meta.Length; i++)
            sb
            .Append(" | ")
            .Append(row[i])
            .Append(' ', width[i] - (row[i]?.Length ?? 0));

            sb.Append(" |");

            result[index++] = sb.ToString();
        }

        return result;
    }

    public void Equal(string table, IEnumerable<T> obj)
    {
        Meta[] meta;
        var aObj = ParseInternal(table, out meta);
        var bObj = obj?.ToArray() ?? new T[0];

        var ab = Print(aObj.Union(bObj).ToArray(), meta);

        MultilineEqual(
            ab.Take(aObj.Count + 1).ToArray(),
            ab.Skip(aObj.Count + 1).Take(bObj.Length).ToArray());
    }

    private void MultilineEqual(string[] a , string[] b)
    {
        if (a == null && b == null) return;

        if (a == null || b == null) return;

        var success = true;

        var result = new string[a.Length + b.Length];

        // Add header of a to result.
        result[0] = string.Concat("   ", a[0]);

        for(var i = 1; i < a.Length; i++)
        {
            var ai = a.Length > i ? a[i] : null;
            var bi = b.Length > i ? b[i-1] : null;

            if (string.Equals(ai, bi, StringComparison.Ordinal))
            {
                result[i] = string.Concat("   ", ai);
                a[i] = null;
                b[i-1] = null;
            }

            else
            {
                var found = false;
                for(var j = 0; j < b.Length; j++)
                {
                    if (string.Equals(ai, b[j], StringComparison.Ordinal))
                    {
                        result[i] = string.Concat("   ", ai);
                        a[i] = null;
                        b[j] = null;
                        found = true;
                        
                        break;
                    }
                }
                
                if (!found)
                {
                    result[i] = string.Concat(" - ", ai);
                    success = false;
                }
            }
        }

        var offset = a.Length;
        foreach(var e in b.Where(x => x != null))
        {
            result[offset++] = string.Concat(" + ", e);
            success = false;
        }

        if (!success)
        throw new ArgumentException(
            string.Concat(
                Environment.NewLine,
                string.Join(Environment.NewLine, result.Where(e => e != null).ToArray())));
    }

    private Meta[] InitMeta(string[] headerCells, Type targetType)
    {
        var meta = new Meta[headerCells.Length];

        for (var i = 0; i < headerCells.Length; i++)
        {
            var header = headerCells[i].Trim();

            var property = targetType.GetProperty(header);

            if (property == null) throw new InvalidOperationException($"Unknown property {header}");

            meta[i] = new Meta(header, property);
        }

        return meta;
    }

    #region Classes
    protected class Meta
    {
        public Meta(string header, PropertyInfo property)
        {
            Header = header;
            Property = property;
        }

        public string Header { get; set; }
        public PropertyInfo Property { get; set; }
    }
    #endregion
}
