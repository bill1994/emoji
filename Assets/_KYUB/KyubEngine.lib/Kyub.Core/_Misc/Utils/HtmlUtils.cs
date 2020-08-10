using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System;
using System.Collections.Specialized;

namespace Kyub
{
    public static class HtmlUtils
    {
        #region Internal Consts

        //Complex Tags
        readonly static OrderedDictionary HTML_CLEAR_PATTERN_POSTPARSE = new OrderedDictionary()
        {
                { @"<font.*?(color=[""'](\S*?)[""'])>((.|\n|\r)*?)<\/font>", "<color={2}>{3}</color>" }, //Group 2 is the font color and group 3 is the text affected
        };

        readonly static OrderedDictionary HTML_CLEAR_PATTERN = new OrderedDictionary()
        {
                { @"&nbsp;", " " },
                { @"<br((.|\n|\r)*?)>", "\n" },
                { @"<div((.|\n|\r)*?)>", "" },
                { @"<[\/]div>", "\n" },
                { @"<p>", "" },
                { @"<p ((.|\n|\r)*?)>", "" },
                { @"<[\/]p>", "\n" },
                { @"<span((.|\n|\r)*?)>", "" },
                { @"<[\/]span>", "" },
                { @"<a>", "" },
                { @"<a ((.|\n|\r)*?)>", "" },
                { @"<[\/]a>", "" },
                { @"<h[1-9]((.|\n|\r)*?)>", "" },
                { @"<[\/]h[1-9]>", "" },
                { @"<img((.|\n|\r)*?)>", "" },
                { @"<[\/]img>", "" },
                { @"<ul((.|\n|\r)*?)>", "" },
                { @"<[\/]ul>", "\n" },
                { @"<li>", "•<indent=10%>" },
                { @"<li ((.|\n|\r)*?)>", "•<indent=10%>" },
                { @"<[\/]li>", "</indent>\n" },
                //{ @"<font.*?(face=(.*))?.*?>", "" },
                //{ @"<[\/]font>", "" },
                { @"<table((.|\n|\r)*?)>", "" },
                { @"<[\/]table>", "\n" },
                { @"<tbody((.|\n|\r)*?)>", "" },
                { @"<[\/]tbody>", "\n" },
                { @"<tr((.|\n|\r)*?)>", "" },
                { @"<[\/]tr>", "\n" },
                { @"<td((.|\n|\r)*?)>", "" },
                { @"<[\/]td>", "\t" },
                { @"<strong((.|\n|\r)*?)>", "<b>" },
                { @"<[\/]strong>", "</b>" },
                { @"<em((.|\n|\r)*?)>", "<i>" },
                { @"<[\/]em>", "</i>" },
                { @"<blockquote((.|\n|\r)*?)>", "<indent=5%><i>" },
                { @"<[\/]blockquote>", "</i></indent>\n" },
                { @"<code((.|\n|\r)*?)>", "<indent=5%><i>" },
                { @"<[\/]code>", "</i></indent>\n" },
                { @"⦁", "•" }, //Convert 'notation spot' (U+2981) to 'bullet' (U+2022)
                { @"<font.*?(face=[""'](\S*?)[""'][>]?).*?>", "<font=\"{2}\">" }, //Group 2 is the font name
                { @"<o:p((.|\n|\r)*?)>(.*?)<[\/]o:p>", "" },
                { @"<!--((.|\n|\r)*?)-->", "" }, //HTML Comments
        };

        #endregion

        #region HTML Public Functions (Static)

        public static string ClearHtmlTags(string p_text)
        {
            return ClearHtmlTags<OrderedDictionary>(p_text, null);
        }

        public static string ClearHtmlTags<TDict>(string p_text, IList<TDict> p_customTags) where TDict : IDictionary
        {
            var v_isCustom = p_customTags != null && p_customTags.Count > 0;
            List<OrderedDictionary> v_tagsDictList = new List<OrderedDictionary>();

            //Create custom clear tags
            if (v_isCustom)
            {
                foreach (var v_dict in p_customTags)
                {
                    if (v_dict != null)
                    {
                        OrderedDictionary v_tagsDict = new OrderedDictionary();
                        foreach (DictionaryEntry v_pair in v_dict)
                        {
                            v_tagsDict[v_pair.Key] = v_pair.Value;
                        }
                        v_tagsDictList.Add(v_tagsDict);
                    }
                }
            }
            else
            {
                v_tagsDictList.Add(HTML_CLEAR_PATTERN);
                v_tagsDictList.Add(HTML_CLEAR_PATTERN_POSTPARSE);
            }
            StringBuilder v_builder = new StringBuilder(p_text);

            foreach (var v_dict in v_tagsDictList)
            {
                v_builder = Kyub.RegexUtils.BulkReplace(v_builder, v_dict);
            }

            return v_builder != null ? v_builder.ToString().Trim() : "";
        }

        #endregion
    }
}
