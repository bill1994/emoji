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

        public LocalizationCsvFileReader(string text)
        {
            _text = text != null ? text : "";
        }

        public LocalizationCsvFileReader(TextAsset asset) : this(asset.text)
        {
        }

        #endregion

        #region Helper Functions

        public virtual Dictionary<string, string> ReadDictionary()
        {
            var rows = CsvUtils.Parse(_text, new CsvConfig(new char[] { ';', '=' }, "\r\n", '\"'));
            Dictionary<string, string> dict = new Dictionary<string, string>();

            foreach (var row in rows)
            {
                if (row == null)
                    continue;

                for (int i = 0; i < row.Length; i++)
                {
                    var cell = row[i] == null? "" : row[i].Trim();
                    //Ignore empty rows of rows that start with // (coment tag)
                    if (string.IsNullOrEmpty(cell))
                        continue;

                    //Comments tag will force ignore next cells in this row
                    if (cell.StartsWith("//"))
                        break;

                    else
                    {
                        var key = Kyub.RegexUtils.BulkReplace(cell, Localization.LocaleManager.s_uselessCharsDict).Trim();
                        var value = row.Length > i + 1 && row[i + 1] != null ? row[i + 1] : "";
                        //Comment value-cell will force empty cell
                        if (value.StartsWith("//"))
                            value = "";
                        dict[key] = Kyub.RegexUtils.BulkReplace(value, Localization.LocaleManager.s_uselessCharsDict).Trim();
                        break;
                    }
                }
            }
            return dict;
        }

        #endregion
    }
}
