#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SaySuomi.Utils
{
    /// <summary>
    /// Minimal forward-only JSON writer for building request bodies by hand.
    ///
    /// For what JsonUtility can not do: runtime-defined keys, or embedding pre-serialized JSON.
    /// Avoids Newtonsoft, which is only a transitive dependency here.
    ///
    /// Write-only, and does not validate nesting. Pass a name inside objects, and none at the
    /// root or in arrays.
    /// </summary>
    /// <example>
    /// <code>
    /// string json = new JsonWriter()
    ///     .BeginObject()
    ///     .String("name", "example")
    ///     .BeginArray("items")
    ///     .BeginObject().Int("id", 1).EndObject()
    ///     .EndArray()
    ///     .EndObject()
    ///     .ToString();
    /// // {"name":"example","items":[{"id":1}]}
    /// </code>
    /// </example>
    public sealed class JsonWriter
    {
        static readonly char[] FloatMarkers = { '.', 'e', 'E' };

        readonly StringBuilder text = new();

        // Per open object/array: whether it has a member yet, i.e. whether the next needs a comma.
        readonly Stack<bool> containers = new();

        /// <summary>Opens an object. <paramref name="name"/> is its key in the enclosing object.</summary>
        public JsonWriter BeginObject(string? name = null)
        {
            WritePrefix(name);
            text.Append('{');
            containers.Push(false);
            return this;
        }

        /// <summary>Closes the current object.</summary>
        public JsonWriter EndObject()
        {
            containers.Pop();
            text.Append('}');
            return this;
        }

        /// <summary>Opens an array. <paramref name="name"/> is its key in the enclosing object.</summary>
        public JsonWriter BeginArray(string? name = null)
        {
            WritePrefix(name);
            text.Append('[');
            containers.Push(false);
            return this;
        }

        /// <summary>Closes the current array.</summary>
        public JsonWriter EndArray()
        {
            containers.Pop();
            text.Append(']');
            return this;
        }

        /// <summary>Writes <c>true</c> or <c>false</c>.</summary>
        public JsonWriter Bool(string name, bool value)
        {
            WritePrefix(name);
            text.Append(value ? "true" : "false");
            return this;
        }

        /// <summary>Writes an integer.</summary>
        public JsonWriter Int(string name, long value)
        {
            WritePrefix(name);
            text.Append(value.ToString(CultureInfo.InvariantCulture));
            return this;
        }

        /// <summary>
        /// Writes a float, always with a decimal point or exponent (1 → 1.0) so strict readers
        /// do not see an int. Throws on NaN and infinity.
        /// </summary>
        public JsonWriter Number(string name, double value)
        {
            if (!double.IsFinite(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "JSON has no NaN or infinity.");
            }

            string number = value.ToString("R", CultureInfo.InvariantCulture);
            if (number.IndexOfAny(FloatMarkers) < 0)
            {
                number += ".0";
            }

            WritePrefix(name);
            text.Append(number);
            return this;
        }

        /// <summary>Writes a string, escaping quotes, backslashes and control characters.</summary>
        public JsonWriter String(string name, string value)
        {
            WritePrefix(name);
            WriteString(value);
            return this;
        }

        /// <summary>Writes pre-serialized JSON unchanged.</summary>
        public JsonWriter Raw(string name, string json)
        {
            WritePrefix(name);
            text.Append(json);
            return this;
        }

        /// <summary>The JSON written so far.</summary>
        public override string ToString() => text.ToString();

        void WritePrefix(string? name)
        {
            if (containers.Count > 0)
            {
                if (containers.Pop())
                {
                    text.Append(',');
                }

                containers.Push(true);
            }

            if (name is not null)
            {
                WriteString(name);
                text.Append(':');
            }
        }

        void WriteString(string value)
        {
            text.Append('"');

            foreach (char c in value)
            {
                switch (c)
                {
                    case '"':
                        text.Append("\\\"");
                        break;
                    case '\\':
                        text.Append("\\\\");
                        break;
                    case '\n':
                        text.Append("\\n");
                        break;
                    case '\r':
                        text.Append("\\r");
                        break;
                    case '\t':
                        text.Append("\\t");
                        break;
                    case < ' ':
                        text.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        break;
                    default:
                        text.Append(c);
                        break;
                }
            }

            text.Append('"');
        }
    }
}
