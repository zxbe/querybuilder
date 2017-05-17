using System;
using System.Collections.Generic;
using System.Linq;

namespace SqlKata.Compilers
{
    public abstract class AbstractCompiler
    {
        public Inflector Inflector { get; protected set; }
        public string TablePrefix { get; set; } = "";
        public string DateFormat { get; set; } = "yyyy-MM-dd HH:mm:ss";
        public bool IsDebug = false;
        protected string separator
        {
            get
            {
                return IsDebug ? "\n" : " ";
            }
        }

        protected string JoinComponents(List<string> components, string section = null)
        {
            return string.Join(separator, components);
        }



        /// <summary>
        /// Wrap a table in keyword identifiers.
        /// </summary>
        /// <param name="table"></param>
        /// <returns></returns>
        public string WrapTable(string table)
        {
            return Wrap(this.TablePrefix + table, true);
        }

        public string Wrap(object value, bool prefixAlias = false)
        {
            if (value is Raw)
            {
                return Wrap((value as Raw));
            }

            if (value is string)
            {
                return Wrap((value as string));
            }

            if (Helper.IsNumber(value))
            {
                return value + "";
            }

            if (value is DateTime)
            {
                return FormatDateTime((DateTime)value);
            }

            return value.ToString();

        }

        /// <summary>
        /// Format input for different types, e.g. DateTime
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public object FormatInput(object value)
        {
            if (value is DateTime)
            {
                return FormatDateTime((DateTime)value);
            }

            return value;
        }

        private string FormatDateTime(DateTime dateTime)
        {
            return dateTime.ToString(DateFormat);
        }

        /// <summary>
        /// Wrap a single string in a column identifier.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public string Wrap(string value, bool prefixAlias = false)
        {
            if (value.ToLower().Contains(" as "))
            {
                var segments = value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (prefixAlias)
                {
                    segments[2] = this.TablePrefix + segments[2];
                }

                return Wrap(segments[0]) + " AS " + WrapValue(segments[2]);
            }

            if (value.Contains("."))
            {
                return string.Join(".", value.Split('.').Select((x, index) =>
                {
                    // Wrap the first segment as table
                    if (index == 0)
                    {
                        return WrapTable(x);
                    }

                    return WrapValue(x);

                }));
            }

            // If we reach here then the value does not contain an "AS" alias 
            // nor dot "." expression, so wrap it as regular value.
            return WrapValue(value);
        }

        public string Wrap(Raw value)
        {
            return value.Value;
        }

        /// <summary>
        /// Wrap a single string in keyword identifiers.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public virtual string WrapValue(string value)
        {
            if (value == "*") return value;

            return '"' + value.Replace("\"", "\"\"") + '"';
        }

        public string Parameter<T>(T value)
        {
            if (value is Raw)
            {
                return (value as Raw).Value;
            }

            return "?";
        }

        /// <summary>
        /// Convert an array of column names into a delimited string.
        /// </summary>
        /// <param name="columns"></param>
        /// <returns></returns>

        public string Columnize(IEnumerable<AbstractColumn> columns)
        {
            var cols = columns.Select(x =>
            {
                if (x is RawColumn)
                {
                    return (x as RawColumn).Expression;
                }

                return Wrap((x as Column).Name);
            });

            return string.Join(", ", cols);
        }

        /// <summary>
        /// Create query parameter place-holders for an array.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public string Parametrize<T>(IEnumerable<T> values)
        {
            return string.Join(", ", values.Select(x => Parameter(x)));
        }

        /// <summary>
        /// Wrap an array of values.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public List<string> WrapArray(List<string> values)
        {
            return values.Select(x => Wrap(x)).ToList();
        }

        public virtual string Singular(string value)
        {
            return Inflector.Singularize(value);
        }

        public virtual string Plural(string value)
        {
            return Inflector.Pluralize(value);
        }
    }
}