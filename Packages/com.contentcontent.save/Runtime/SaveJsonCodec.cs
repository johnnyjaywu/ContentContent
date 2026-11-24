using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ContentContent.Save
{
    /// <summary>
    /// Zero-dependency RFC-8259 JSON serializer and parser for <see cref="SaveDocument"/>.
    /// Preserves exact <see cref="SaveValueType"/> tags so integers, longs, floats, doubles,
    /// booleans, and strings round-trip deterministically under <see cref="CultureInfo.InvariantCulture"/>.
    /// </summary>
    public static class SaveJsonCodec
    {
        public static string Encode(SaveDocument document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            var sb = new StringBuilder(512);
            sb.Append("{\n");
            sb.Append("  \"schemaVersion\": ").Append(document.SchemaVersion.ToString(CultureInfo.InvariantCulture)).Append(",\n");

            SaveMetadata meta = document.Metadata;
            sb.Append("  \"metadata\": {\n");
            sb.Append("    \"slotId\": "); AppendEscapedString(sb, meta.SlotId); sb.Append(",\n");
            sb.Append("    \"displayName\": "); AppendEscapedString(sb, meta.DisplayName); sb.Append(",\n");
            sb.Append("    \"schemaVersion\": ").Append(meta.SchemaVersion.ToString(CultureInfo.InvariantCulture)).Append(",\n");
            sb.Append("    \"savedAtUtcTicks\": ").Append(meta.SavedAtUtcTicks.ToString(CultureInfo.InvariantCulture)).Append(",\n");
            sb.Append("    \"playTimeSeconds\": ").Append(meta.PlayTimeSeconds.ToString("R", CultureInfo.InvariantCulture)).Append(",\n");
            sb.Append("    \"locationLabel\": "); AppendEscapedString(sb, meta.LocationLabel); sb.Append('\n');
            sb.Append("  },\n");

            sb.Append("  \"sections\": {");
            var sectionKeys = new List<string>(document.Sections.Keys);
            sectionKeys.Sort(StringComparer.Ordinal);
            if (sectionKeys.Count > 0)
            {
                sb.Append('\n');
                for (int s = 0; s < sectionKeys.Count; s++)
                {
                    string sectionId = sectionKeys[s];
                    SaveSection section = document.Sections[sectionId];
                    sb.Append("    ");
                    AppendEscapedString(sb, sectionId);
                    sb.Append(": {");

                    var entryKeys = new List<string>(section.Values.Keys);
                    entryKeys.Sort(StringComparer.Ordinal);
                    if (entryKeys.Count > 0)
                    {
                        sb.Append('\n');
                        for (int k = 0; k < entryKeys.Count; k++)
                        {
                            string entryKey = entryKeys[k];
                            SaveValue val = section.Values[entryKey];
                            sb.Append("      ");
                            AppendEscapedString(sb, entryKey);
                            sb.Append(": { \"type\": ");
                            AppendEscapedString(sb, val.Type.ToString());
                            sb.Append(", \"v\": ");
                            AppendValueJson(sb, in val);
                            sb.Append(k + 1 < entryKeys.Count ? " },\n" : " }\n");
                        }
                        sb.Append("    }");
                    }
                    else
                    {
                        sb.Append('}');
                    }

                    sb.Append(s + 1 < sectionKeys.Count ? ",\n" : "\n");
                }
                sb.Append("  }\n");
            }
            else
            {
                sb.Append("}\n");
            }

            sb.Append('}');
            return sb.ToString();
        }

        public static bool TryDecode(string json, out SaveDocument document)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                document = null;
                return false;
            }

            try
            {
                document = Decode(json);
                return document != null;
            }
            catch (FormatException)
            {
                document = null;
                return false;
            }
        }

        public static SaveDocument Decode(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new FormatException("Save JSON payload cannot be empty.");

            var parser = new Parser(json);
            SaveDocument doc = parser.ParseDocument();
            parser.SkipWhitespace();
            if (!parser.IsAtEnd)
                throw new FormatException("Unexpected trailing data after JSON root object.");

            return doc;
        }

        private static void AppendValueJson(StringBuilder sb, in SaveValue val)
        {
            switch (val.Type)
            {
                case SaveValueType.Bool:
                    sb.Append(val.BoolValue ? "true" : "false");
                    break;
                case SaveValueType.Int:
                    sb.Append(val.IntValue.ToString(CultureInfo.InvariantCulture));
                    break;
                case SaveValueType.Long:
                    sb.Append(val.LongValue.ToString(CultureInfo.InvariantCulture));
                    break;
                case SaveValueType.Float:
                    sb.Append(val.FloatValue.ToString("R", CultureInfo.InvariantCulture));
                    break;
                case SaveValueType.Double:
                    sb.Append(val.DoubleValue.ToString("R", CultureInfo.InvariantCulture));
                    break;
                case SaveValueType.String:
                    AppendEscapedString(sb, val.StringValue);
                    break;
                default:
                    sb.Append("null");
                    break;
            }
        }

        private static void AppendEscapedString(StringBuilder sb, string value)
        {
            sb.Append('"');
            if (!string.IsNullOrEmpty(value))
            {
                for (int i = 0; i < value.Length; i++)
                {
                    char c = value[i];
                    switch (c)
                    {
                        case '"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '\b': sb.Append("\\b"); break;
                        case '\f': sb.Append("\\f"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            if (c < ' ')
                            {
                                sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                            }
                            else
                            {
                                sb.Append(c);
                            }
                            break;
                    }
                }
            }
            sb.Append('"');
        }

        private struct Parser
        {
            private readonly string _json;
            private int _pos;

            public bool IsAtEnd => _pos >= _json.Length;

            public Parser(string json)
            {
                _json = json;
                _pos = 0;
            }

            public SaveDocument ParseDocument()
            {
                Expect('{');
                int schemaVersion = 1;
                var metadata = new SaveMetadata { SchemaVersion = 1 };
                var sections = new Dictionary<string, SaveSection>(StringComparer.Ordinal);
                bool hasSchema = false;

                SkipWhitespace();
                if (TryConsume('}'))
                    throw new FormatException("Save document JSON object is empty.");

                while (true)
                {
                    string key = ReadString();
                    Expect(':');

                    if (string.Equals(key, "schemaVersion", StringComparison.Ordinal))
                    {
                        schemaVersion = ReadInt();
                        hasSchema = true;
                    }
                    else if (string.Equals(key, "metadata", StringComparison.Ordinal))
                    {
                        metadata = ParseMetadata();
                    }
                    else if (string.Equals(key, "sections", StringComparison.Ordinal))
                    {
                        ParseSections(sections);
                    }
                    else
                    {
                        SkipJsonValue();
                    }

                    SkipWhitespace();
                    if (TryConsume(','))
                        continue;

                    Expect('}');
                    break;
                }

                if (!hasSchema && metadata.SchemaVersion > 0)
                    schemaVersion = metadata.SchemaVersion;

                var doc = new SaveDocument(schemaVersion) { Metadata = metadata };
                foreach (KeyValuePair<string, SaveSection> kvp in sections)
                {
                    doc.SetSection(kvp.Key, kvp.Value);
                }
                return doc;
            }

            private SaveMetadata ParseMetadata()
            {
                Expect('{');
                var meta = new SaveMetadata();
                SkipWhitespace();
                if (TryConsume('}'))
                    return meta;

                while (true)
                {
                    string key = ReadString();
                    Expect(':');
                    switch (key)
                    {
                        case "slotId": meta.SlotId = ReadStringOrNull(); break;
                        case "displayName": meta.DisplayName = ReadStringOrNull(); break;
                        case "schemaVersion": meta.SchemaVersion = ReadInt(); break;
                        case "savedAtUtcTicks": meta.SavedAtUtcTicks = ReadLong(); break;
                        case "playTimeSeconds": meta.PlayTimeSeconds = ReadFloat(); break;
                        case "locationLabel": meta.LocationLabel = ReadStringOrNull(); break;
                        default: SkipJsonValue(); break;
                    }

                    SkipWhitespace();
                    if (TryConsume(','))
                        continue;
                    Expect('}');
                    break;
                }

                return meta;
            }

            private void ParseSections(Dictionary<string, SaveSection> sections)
            {
                Expect('{');
                SkipWhitespace();
                if (TryConsume('}'))
                    return;

                while (true)
                {
                    string sectionId = ReadString();
                    Expect(':');
                    SaveSection section = ParseSection();
                    sections[sectionId] = section;

                    SkipWhitespace();
                    if (TryConsume(','))
                        continue;
                    Expect('}');
                    break;
                }
            }

            private SaveSection ParseSection()
            {
                Expect('{');
                var section = new SaveSection();
                SkipWhitespace();
                if (TryConsume('}'))
                    return section;

                while (true)
                {
                    string entryKey = ReadString();
                    Expect(':');
                    SaveValue value = ParseTypedSaveValue();
                    if (value.Type != SaveValueType.None)
                        section.SetValue(entryKey, value);

                    SkipWhitespace();
                    if (TryConsume(','))
                        continue;
                    Expect('}');
                    break;
                }

                return section;
            }

            private SaveValue ParseTypedSaveValue()
            {
                Expect('{');
                SaveValueType valueType = SaveValueType.None;
                string rawBool = null;
                string rawNumber = null;
                string rawString = null;
                bool hasValueToken = false;

                SkipWhitespace();
                if (!TryConsume('}'))
                {
                    while (true)
                    {
                        string field = ReadString();
                        Expect(':');

                        if (string.Equals(field, "type", StringComparison.Ordinal))
                        {
                            string typeStr = ReadString();
                            if (!Enum.TryParse(typeStr, true, out valueType))
                                throw new FormatException($"Unknown SaveValueType '{typeStr}'.");
                        }
                        else if (string.Equals(field, "v", StringComparison.Ordinal))
                        {
                            hasValueToken = true;
                            SkipWhitespace();
                            char peek = PeekChar();
                            if (peek == '"')
                            {
                                rawString = ReadString();
                            }
                            else if (peek == 't' || peek == 'f')
                            {
                                rawBool = ReadLiteral();
                            }
                            else if (peek == 'n')
                            {
                                ReadLiteral();
                            }
                            else
                            {
                                rawNumber = ReadNumberToken();
                            }
                        }
                        else
                        {
                            SkipJsonValue();
                        }

                        SkipWhitespace();
                        if (TryConsume(','))
                            continue;
                        Expect('}');
                        break;
                    }
                }

                if (!hasValueToken || valueType == SaveValueType.None)
                    return SaveValue.None;

                switch (valueType)
                {
                    case SaveValueType.Bool:
                        return SaveValue.FromBool(string.Equals(rawBool, "true", StringComparison.Ordinal));
                    case SaveValueType.Int:
                        return SaveValue.FromInt(int.Parse(rawNumber, NumberStyles.Integer, CultureInfo.InvariantCulture));
                    case SaveValueType.Long:
                        return SaveValue.FromLong(long.Parse(rawNumber, NumberStyles.Integer, CultureInfo.InvariantCulture));
                    case SaveValueType.Float:
                        return SaveValue.FromFloat(float.Parse(rawNumber, NumberStyles.Float, CultureInfo.InvariantCulture));
                    case SaveValueType.Double:
                        return SaveValue.FromDouble(double.Parse(rawNumber, NumberStyles.Float, CultureInfo.InvariantCulture));
                    case SaveValueType.String:
                        return SaveValue.FromString(rawString ?? string.Empty);
                    default:
                        return SaveValue.None;
                }
            }

            public void SkipWhitespace()
            {
                while (_pos < _json.Length && char.IsWhiteSpace(_json[_pos]))
                    _pos++;
            }

            private char PeekChar()
            {
                if (_pos >= _json.Length)
                    throw new FormatException("Unexpected end of JSON input.");
                return _json[_pos];
            }

            private void Expect(char expected)
            {
                SkipWhitespace();
                if (_pos >= _json.Length || _json[_pos] != expected)
                    throw new FormatException($"Expected '{expected}' at position {_pos}.");
                _pos++;
            }

            private bool TryConsume(char c)
            {
                SkipWhitespace();
                if (_pos < _json.Length && _json[_pos] == c)
                {
                    _pos++;
                    return true;
                }
                return false;
            }

            private string ReadStringOrNull()
            {
                SkipWhitespace();
                if (PeekChar() == 'n')
                {
                    string lit = ReadLiteral();
                    if (!string.Equals(lit, "null", StringComparison.Ordinal))
                        throw new FormatException($"Unexpected literal '{lit}'.");
                    return string.Empty;
                }
                return ReadString();
            }

            private string ReadString()
            {
                Expect('"');
                var sb = new StringBuilder(32);
                while (_pos < _json.Length)
                {
                    char c = _json[_pos++];
                    if (c == '"')
                        return sb.ToString();

                    if (c == '\\')
                    {
                        if (_pos >= _json.Length)
                            throw new FormatException("Unterminated string escape sequence.");

                        char esc = _json[_pos++];
                        switch (esc)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case '/': sb.Append('/'); break;
                            case 'b': sb.Append('\b'); break;
                            case 'f': sb.Append('\f'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            case 'u':
                                if (_pos + 4 > _json.Length)
                                    throw new FormatException("Invalid unicode escape in JSON string.");
                                string hex = _json.Substring(_pos, 4);
                                _pos += 4;
                                sb.Append((char)Convert.ToInt32(hex, 16));
                                break;
                            default:
                                throw new FormatException($"Invalid escape sequence '\\{esc}' in JSON string.");
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }

                throw new FormatException("Unterminated JSON string.");
            }

            private int ReadInt() => int.Parse(ReadNumberToken(), NumberStyles.Integer, CultureInfo.InvariantCulture);
            private long ReadLong() => long.Parse(ReadNumberToken(), NumberStyles.Integer, CultureInfo.InvariantCulture);
            private float ReadFloat() => float.Parse(ReadNumberToken(), NumberStyles.Float, CultureInfo.InvariantCulture);

            private string ReadNumberToken()
            {
                SkipWhitespace();
                int start = _pos;
                while (_pos < _json.Length)
                {
                    char c = _json[_pos];
                    if ((c >= '0' && c <= '9') || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E')
                    {
                        _pos++;
                    }
                    else
                    {
                        break;
                    }
                }

                if (_pos == start)
                    throw new FormatException($"Expected JSON number at position {_pos}.");

                return _json.Substring(start, _pos - start);
            }

            private string ReadLiteral()
            {
                SkipWhitespace();
                int start = _pos;
                while (_pos < _json.Length && char.IsLetter(_json[_pos]))
                    _pos++;
                return _json.Substring(start, _pos - start);
            }

            private void SkipJsonValue()
            {
                SkipWhitespace();
                char c = PeekChar();
                if (c == '"')
                {
                    ReadString();
                }
                else if (c == '{')
                {
                    _pos++;
                    SkipWhitespace();
                    if (TryConsume('}'))
                        return;
                    while (true)
                    {
                        ReadString();
                        Expect(':');
                        SkipJsonValue();
                        if (TryConsume(','))
                            continue;
                        Expect('}');
                        break;
                    }
                }
                else if (c == '[')
                {
                    _pos++;
                    SkipWhitespace();
                    if (TryConsume(']'))
                        return;
                    while (true)
                    {
                        SkipJsonValue();
                        if (TryConsume(','))
                            continue;
                        Expect(']');
                        break;
                    }
                }
                else if (c == 't' || c == 'f' || c == 'n')
                {
                    ReadLiteral();
                }
                else
                {
                    ReadNumberToken();
                }
            }
        }
    }
}
