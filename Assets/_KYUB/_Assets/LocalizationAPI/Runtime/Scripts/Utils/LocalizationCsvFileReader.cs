using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
using UnityEngine;

namespace Kyub.Localization
{
    public class LocalizationCsvFileReader
    {
        #region Private Variables

        protected string _text = "";

        #endregion

        #region Constructors

        public LocalizationCsvFileReader(string p_text)
        {
            _text = p_text != null ? p_text : "";
        }

        public LocalizationCsvFileReader(TextAsset p_asset) : this(p_asset.text)
        {
        }

        #endregion

        #region Helper Functions

        public virtual Dictionary<string, string> ReadDictionary()
        {
            var v_rows = CsvUtils.Parse(_text, new CsvConfig(new char[] { ';', '=' }, "\r\n", '\"'));
            Dictionary<string, string> v_dict = new Dictionary<string, string>();

            foreach (var v_row in v_rows)
            {
                if (v_row == null)
                    continue;

                for (int i = 0; i < v_row.Length; i++)
                {
                    var v_cell = v_row[i] == null? "" : v_row[i].Trim();
                    //Ignore empty rows of rows that start with // (coment tag)
                    if (string.IsNullOrEmpty(v_cell))
                        continue;

                    //Comments tag will force ignore next cells in this row
                    if (v_cell.StartsWith("//"))
                        break;

                    else
                    {
                        var v_key = Kyub.RegexUtils.BulkReplace(v_cell, Localization.LocaleManager.s_uselessCharsDict).Trim();
                        var v_value = v_row.Length > i + 1 && v_row[i + 1] != null ? v_row[i + 1] : "";
                        //Comment value-cell will force empty cell
                        if (v_value.StartsWith("//"))
                            v_value = "";
                        v_dict[v_key] = Kyub.RegexUtils.BulkReplace(v_value, Localization.LocaleManager.s_uselessCharsDict).Trim();
                        break;
                    }
                }
            }
            return v_dict;
        }

        #endregion
    }

    #region Internal Helper Classes

    internal static class CsvUtils
    {
        public static List<string[]> Parse(string csvFileContents, CsvConfig p_config = null)
        {
            List<string[]> v_rows = new List<string[]>();
            var reader = new CsvReader(p_config);
            try
            {
                var v_counter = 0;
                foreach (var row in reader.Read(csvFileContents))
                {
                    v_rows.Add(row);
                    v_counter++;
                }
            }
            catch (Exception p_exception)
            {
                Debug.Log(p_exception.Message);
            }
            return v_rows;
        }
    }

    internal class CsvConfig
    {
        public HashSet<char> Delimiters { get; private set; }
        public string NewLineMark { get; private set; }
        public char QuotationMark { get; private set; }

        public CsvConfig(char delimiter, string newLineMark, char quotationMark)
        {
            Delimiters = new HashSet<char>() { delimiter };
            NewLineMark = newLineMark;
            QuotationMark = quotationMark;
        }

        public CsvConfig(ICollection<Char> delimiters, string newLineMark, char quotationMark)
        {
            Delimiters = delimiters != null && delimiters.Count > 0? new HashSet<char>(delimiters) : new HashSet<char>() { ';' };
            NewLineMark = newLineMark;
            QuotationMark = quotationMark;
        }

        // useful configs

        public static CsvConfig Default
        {
            get { return new CsvConfig(';', "\r\n", '\"'); }
        }

        // etc.
    }

    internal class CsvReader
    {
        private CsvConfig m_config;

        public CsvReader(CsvConfig config = null)
        {
            if (config == null)
                m_config = CsvConfig.Default;
            else
                m_config = config;
        }

        public IEnumerable<string[]> Read(string csvFileContents)
        {
            //using (StringReader reader = new StringReader(csvFileContents))
            //{
            int i = 0;
            while (true)
            {
                //string line = reader.ReadLine();
                if (csvFileContents == null || i >= csvFileContents.Length)
                    break;
                yield return ParseLine(csvFileContents, ref i);
            }
            //}
        }

        private string[] ParseLine(string data, ref int i)
        {
            Stack<string> result = new Stack<string>();

            //int i = 0;
            while (true)
            {
                string cell = ParseNextCell(data, ref i);
                if (cell == null)
                    break;
                //Force remove escaped double quotemark to simple quotemark
                cell = cell.Trim().Replace("\"\"", "\"");
                result.Push(cell);
            }

            // remove last elements if they're empty
            while (result.Count > 0 && string.IsNullOrEmpty(result.Peek()))
            {
                result.Pop();
            }

            var resultAsArray = result.ToArray();
            Array.Reverse(resultAsArray);
            return resultAsArray;
        }

        // returns iterator after delimiter or after end of string
        private string ParseNextCell(string data, ref int i)
        {
            if (i >= data.Length)
                return null;

            //Find Line Breaks
            var v_matchCount = IsLineBreak(data, i);
            if (v_matchCount > 0)
            {
                i += v_matchCount;
                return null;
            }

            if (data[i] != m_config.QuotationMark)
                return ParseNotEscapedCell(data, ref i);
            else
                return ParseEscapedCell(data, ref i);
        }

        protected int IsLineBreak(string data, int i)
        {
            //Find Line Breaks
            var v_matchCount = 0;
            for (int j = 0; j < m_config.NewLineMark.Length; j++)
            {
                if (data[i + j] == m_config.NewLineMark[j])
                    v_matchCount++;
                else
                    break;
            }
            if (v_matchCount == m_config.NewLineMark.Length)
            {
                //i += v_matchCount;
                return v_matchCount;
            }
            return 0;
        }

        // returns iterator after delimiter or after end of string
        private string ParseNotEscapedCell(string data, ref int i)
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                if (i >= data.Length) // return iterator after end of string
                    break;
                if (m_config.Delimiters.Contains(data[i]))
                {
                    i++; // return iterator after delimiter
                    break;
                }
                if (IsLineBreak(data, i) > 0)
                {
                    // return iterator after line break
                    break;
                }
                sb.Append(data[i]);
                i++;
            }
            return sb.ToString();
        }

        // returns iterator after delimiter or after end of string
        private string ParseEscapedCell(string data, ref int i)
        {
            i++; // omit first character (quotation mark)
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                if (i >= data.Length)
                    break;
                if (data[i] == m_config.QuotationMark)
                {
                    i++; // we're more interested in the next character
                    if (i >= data.Length)
                    {
                        // quotation mark was closing cell;
                        // return iterator after end of string
                        break;
                    }
                    if (m_config.Delimiters.Contains(data[i]))
                    {
                        // quotation mark was closing cell;
                        // return iterator after delimiter
                        i++;
                        break;
                    }
                    if (data[i] == m_config.QuotationMark)
                    {
                        // it was doubled (escaped) quotation mark;
                        // do nothing -- we've already skipped first quotation mark
                    }
                    if (IsLineBreak(data, i) > 0)
                    {
                        // return iterator after line break
                        break;
                    }
                }
                sb.Append(data[i]);
                i++;
            }

            return sb.ToString();
        }
    }

    #endregion
}
