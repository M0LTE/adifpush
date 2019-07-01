using System;
using System.Collections.Generic;
using System.Linq;

namespace adifpush
{
    class AdifRecord
    {
        private enum ParseState
        {
            LookingForStartOfRecord, FieldName, FieldLen, Data
        }

        public Dictionary<string, string> Fields { get; private set; } = new Dictionary<string, string>();

        public override string ToString()
        {
            return String.Join(' ', Fields.Select(f => $"<{f.Key}:{f.Value.Length}>{f.Value}")) + " <eor>";
        }

        public static bool TryParse(string record, out AdifRecord adifRecord, out string error)
        {
            adifRecord = new AdifRecord();

            ParseState p = ParseState.LookingForStartOfRecord;
            var fieldNameBuffer = new List<char>();
            var fieldLenBuffer = new List<char>();
            var dataBuffer = new List<char>();

            try
            {
                for (int i = 0; i < record.Length; i++)
                {
                    if (p == ParseState.LookingForStartOfRecord)
                    {
                        if (record[i] == '<')
                        {
                            p = ParseState.FieldName;
                        }
                        else continue;
                    }
                    else if (p == ParseState.FieldName)
                    {
                        if (record[i] == ':')
                        {
                            p = ParseState.FieldLen;
                        }
                        else
                        {
                            fieldNameBuffer.Add(record[i]);
                        }
                    }
                    else if (p == ParseState.FieldLen)
                    {
                        if (record[i] == '>')
                        {
                            p = ParseState.Data;
                        }
                        else
                        {
                            fieldLenBuffer.Add(record[i]);
                        }
                    }
                    else if (p == ParseState.Data)
                    {
                        int fieldLen = int.Parse(new String(fieldLenBuffer.ToArray()));
                        if (dataBuffer.Count == fieldLen)
                        {
                            string fieldName = new string(fieldNameBuffer.ToArray());
                            string data = new string(dataBuffer.ToArray());

                            adifRecord.Fields.Add(fieldName, data);

                            fieldNameBuffer.Clear();
                            fieldLenBuffer.Clear();
                            dataBuffer.Clear();
                            p = ParseState.LookingForStartOfRecord;
                        }
                        else
                        {
                            dataBuffer.Add(record[i]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }

            error = null;
            return true;
        }
    }
}
