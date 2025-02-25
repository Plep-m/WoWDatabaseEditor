﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using WDBXEditor.Reader;
using WDBXEditor.Reader.FileTypes;
using static WDBXEditor.Common.Constants;

namespace WDBXEditor.Storage
{
    public class DBEntry : IDisposable
    {
        private int max = -1;
        
        private int min = -1;
        private IEnumerable<int> primaryKeys;
        //private IEnumerable<int> unqiueRowIndices;

        public DBEntry(DBHeader header, string filepath)
        {
            Header = header;
            FilePath = filepath;
            SavePath = filepath;
            Header.TableStructure = Database.Definitions.Tables.FirstOrDefault(x =>
                x.Name.Equals(Path.GetFileNameWithoutExtension(filepath), Ignorecase) && x.Build == Database.BuildNumber);

            LoadDefinition();
        }

        public DBHeader Header { get; }
        public DataTable Data { get; set; }
        public bool Changed { get; set; } = false;
        public string FilePath { get; }
        public string FileName => Path.GetFileName(FilePath);
        public string SavePath { get; set; }
        public Table TableStructure => Header.TableStructure;

        public string Key { get; private set; }
        public int Build { get; private set; }
        public string BuildName { get; private set; }
        public string Tag { get; private set; }

        public void Dispose()
        {
            Data?.Dispose();
            Data = null;
        }


        /// <summary>
        ///     Converts the XML definition to an empty DataTable
        /// </summary>
        public void LoadDefinition()
        {
            if (TableStructure == null)
                return;

            Build = TableStructure.Build;
            Key = TableStructure.Key.Name;
            BuildName = BuildText(Build);
            Tag = Guid.NewGuid().ToString();

            //Column name check
            if (TableStructure.Fields.GroupBy(x => x.Name).Any(y => y.Count() > 1))
            {
                //MessageBox.Show($"Duplicate column names for {FileName} - {Build} definition");
                return;
            }

            LoadTableStructure();
        }

        public void LoadTableStructure()
        {
            Data = new DataTable {TableName = Tag, CaseSensitive = false, RemotingFormat = SerializationFormat.Binary};

            int localizationCount = Build <= (int) ExpansionFinalBuild.Classic ? 9 : 17; //Pre TBC had 9 locales

            foreach (Field col in TableStructure.Fields)
            {
                var languages = new Queue<TextWowEnum>(Enum.GetValues(typeof(TextWowEnum)).Cast<TextWowEnum>());
                string[] columnsNames = col.ColumnNames.Split(',');

                for (var i = 0; i < col.ArraySize; i++)
                {
                    string columnName = col.Name;

                    if (col.ArraySize > 1)
                    {
                        if (columnsNames.Length >= i + 1 && !string.IsNullOrWhiteSpace(columnsNames[i]))
                            columnName = columnsNames[i];
                        else
                            columnName += "_" + (i + 1);
                    }

                    col.InternalName = columnName;

                    switch (col.Type.ToLower())
                    {
                        case "sbyte":
                            Data.Columns.Add(columnName, typeof(sbyte));
                            Data.Columns[columnName].DefaultValue = 0;
                            break;
                        case "byte":
                            Data.Columns.Add(columnName, typeof(byte));
                            Data.Columns[columnName].DefaultValue = 0;
                            break;
                        case "int32":
                        case "int":
                            Data.Columns.Add(columnName, typeof(int));
                            Data.Columns[columnName].DefaultValue = 0;
                            break;
                        case "uint32":
                        case "uint":
                            Data.Columns.Add(columnName, typeof(uint));
                            Data.Columns[columnName].DefaultValue = 0;
                            break;
                        case "int64":
                        case "long":
                            Data.Columns.Add(columnName, typeof(long));
                            Data.Columns[columnName].DefaultValue = 0;
                            break;
                        case "uint64":
                        case "ulong":
                            Data.Columns.Add(columnName, typeof(ulong));
                            Data.Columns[columnName].DefaultValue = 0;
                            break;
                        case "single":
                        case "float":
                            Data.Columns.Add(columnName, typeof(float));
                            Data.Columns[columnName].DefaultValue = 0;
                            break;
                        case "boolean":
                        case "bool":
                            Data.Columns.Add(columnName, typeof(bool));
                            Data.Columns[columnName].DefaultValue = 0;
                            break;
                        case "string":
                            Data.Columns.Add(columnName, typeof(string));
                            Data.Columns[columnName].DefaultValue = string.Empty;
                            break;
                        case "int16":
                        case "short":
                            Data.Columns.Add(columnName, typeof(short));
                            Data.Columns[columnName].DefaultValue = 0;
                            break;
                        case "uint16":
                        case "ushort":
                            Data.Columns.Add(columnName, typeof(ushort));
                            Data.Columns[columnName].DefaultValue = 0;
                            break;
                        case "loc":
                            //Special case for localized strings, build up all locales and add string mask
                            for (var x = 0; x < localizationCount; x++)
                            {
                                if (x == localizationCount - 1)
                                {
                                    Data.Columns.Add(col.Name + "_Mask", typeof(uint)); //Last column is a mask
                                    Data.Columns[col.Name + "_Mask"].AllowDBNull = false;
                                    Data.Columns[col.Name + "_Mask"].DefaultValue = 0;
                                }
                                else
                                {
                                    columnName = col.Name + "_" + languages.Dequeue(); //X columns for local strings
                                    Data.Columns.Add(columnName, typeof(string));
                                    Data.Columns[columnName].AllowDBNull = false;
                                    Data.Columns[columnName].DefaultValue = string.Empty;
                                }
                            }

                            break;
                        default:
                            throw new Exception($"Unknown field type {col.Type} for {col.Name}.");
                    }

                    //AutoGenerated Id for CharBaseInfo
                    if (col.AutoGenerate)
                    {
                        Data.Columns[0].ExtendedProperties.Add(AutoGenerated, true);
                        Header.AutoGeneratedColumns++;
                    }

                    Data.Columns[columnName].AllowDBNull = false;
                }
            }

            //Setup the Primary Key
            Data.Columns[Key].DefaultValue = null; //Clear default value
            Data.PrimaryKey = new[] {Data.Columns[Key]};
            Data.Columns[Key].AutoIncrement = true;
            Data.Columns[Key].Unique = true;
        }

        public void Attach()
        {
            if (Data != null && Data.Rows.Count > 0)
                return;

            using (FileStream fs = new(Path.Combine(TempFolder, Tag + ".cache"), FileMode.Open))
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(fs,
                Tag,
                fs.Length,
                MemoryMappedFileAccess.ReadWrite,
                HandleInheritability.None,
                false))
            using (MemoryMappedViewStream stream = mmf.CreateViewStream(0, fs.Length, MemoryMappedFileAccess.Read))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                Data = (DataTable) formatter.Deserialize(stream);
            }
        }


        /// <summary>
        ///     Checks if the file is of Name and Expansion
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="expansion"></param>
        /// <returns></returns>
        public bool IsFileOf(string filename, Expansion expansion)
        {
            return TableStructure.Name.Equals(filename, Ignorecase) && IsBuild(Build, expansion);
        }

        public bool IsFileOf(string filename)
        {
            return TableStructure.Name.Equals(filename, Ignorecase);
        }


        /// <summary>
        ///     Generates a Bit map for all columns as the Blizzard one combines array columns
        /// </summary>
        /// <returns></returns>
        public FieldStructureEntry[] GetBits()
        {
            if (!Header.IsTypeOf<WDB5>())
                return new FieldStructureEntry[Data.Columns.Count];

            var bits = new List<FieldStructureEntry>();
            if (Header is WDC1 header)
            {
                var fields = header.ColumnMeta;
                for (var i = 0; i < fields.Count; i++)
                {
                    var bitcount =
                        (short) (Header.FieldStructure[i].BitCount == 64 ? Header.FieldStructure[i].BitCount : 0); // force bitcounts
                    for (var x = 0; x < fields[i].ArraySize; x++)
                        bits.Add(new FieldStructureEntry(bitcount, 0));
                }
            }
            else
            {
                var fields = Header.FieldStructure;
                for (var i = 0; i < TableStructure.Fields.Count; i++)
                {
                    Field f = TableStructure.Fields[i];
                    for (var x = 0; x < f.ArraySize; x++)
                        bits.Add(new FieldStructureEntry(fields[i]?.Bits ?? 0, 0, fields[i]?.CommonDataType ?? 0xFF));
                }
            }

            return bits.ToArray();
        }

        public int[] GetPadding()
        {
            var padding = new int[Data.Columns.Count];

            var bytecounts = new Dictionary<Type, int>
            {
                {typeof(byte), 1},
                {typeof(short), 2},
                {typeof(ushort), 2}
            };

            if (Header is WDC1 header)
            {
                var c = 0;

                foreach (ColumnStructureEntry field in header.ColumnMeta)
                {
                    Type type = Data.Columns[c].DataType;
                    bool isneeded = field.CompressionType >= CompressionType.Sparse;

                    if (bytecounts.ContainsKey(type) && isneeded)
                    {
                        for (var x = 0; x < field.ArraySize; x++)
                            padding[c++] = 4 - bytecounts[type];
                    }
                    else
                        c += field.ArraySize;
                }
            }

            return padding;
        }

        public void UpdateColumnTypes()
        {
            if (!Header.IsTypeOf<WDB6>())
                return;

            var fields = ((WDB6) Header).FieldStructure;
            var c = 0;
            for (var i = 0; i < TableStructure.Fields.Count; i++)
            {
                int arraySize = TableStructure.Fields[i].ArraySize;

                if (!fields[i].CommonDataColumn)
                {
                    c += arraySize;
                    continue;
                }

                Type columnType;
                switch (fields[i].CommonDataType)
                {
                    case 0:
                        columnType = typeof(string);
                        break;
                    case 1:
                        columnType = typeof(ushort);
                        break;
                    case 2:
                        columnType = typeof(byte);
                        break;
                    case 3:
                        columnType = typeof(float);
                        break;
                    case 4:
                        columnType = typeof(int);
                        break;
                    default:
                        c += arraySize;
                        continue;
                }

                for (var x = 0; x < arraySize; x++)
                {
                    Data.Columns[c].DataType = columnType;
                    c++;
                }
            }
        }

        /// <summary>
        ///     Gets the Min and Max ids
        /// </summary>
        /// <returns></returns>
        public Tuple<int, int> MinMax()
        {
            if (min == -1 || max == -1)
            {
                min = int.MaxValue;
                max = int.MinValue;
                foreach (DataRow dr in Data.Rows)
                {
                    var val = dr.Field<int>(Key);
                    min = Math.Min(min, val);
                    max = Math.Max(max, val);
                }
            }

            return new Tuple<int, int>(min, max);
        }

        /// <summary>
        ///     Gets a list of Ids
        /// </summary>
        /// <returns></returns>
        public IEnumerable<int> GetPrimaryKeys()
        {
            if (primaryKeys == null)
                primaryKeys = Data.AsEnumerable().Select(x => x.Field<int>(Key));

            return primaryKeys;
        }
    }
}