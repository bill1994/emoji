using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System;

namespace Kyub
{
    public static class RegexUtils
    {
        #region Public Functions

        public static StringBuilder BulkReplace<TDict>(StringBuilder p_stringBuilderSource, TDict p_replacementMap) where TDict : IDictionary
        {
            if (p_stringBuilderSource.Length == 0 || p_replacementMap.Count == 0)
            {
                return p_stringBuilderSource;
            }

            string v_replaced = BulkReplace(p_stringBuilderSource.ToString(), p_replacementMap);
            return p_stringBuilderSource.Clear().Append(v_replaced);
        }


        public static string BulkReplace<TDict>(string p_stringSource, TDict p_replacementMap) where TDict : IDictionary
        {
            if (string.IsNullOrEmpty(p_stringSource) || p_replacementMap == null || p_replacementMap.Count == 0)
            {
                return p_stringSource;
            }

            //Convert to List<string> without losing @string Keys or @string Values and create GroupNames for each key entry
            List<string> v_strKeys = new List<string>();
            List<string> v_strValues = new List<string>();
            Dictionary<string, int> v_groupNameToKeyIndex = new Dictionary<string, int>();

            var v_groupHash = UnityEngine.Random.Range(0, int.MaxValue);
            foreach (DictionaryEntry v_pair in p_replacementMap)
            {
                var v_strKey = v_pair.Key == null ? "" : (v_pair.Key is string ? v_pair.Key as string : v_pair.Key.ToString());
                if (!string.IsNullOrEmpty(v_strKey))
                {
                    //Create group name and map it to discover the original key or value to replace
                    var v_groupName = string.Format("BRR_{0}_{1}", v_groupHash, v_strKeys.Count);

                    //Format key to include GroupName in capture
                    v_strKey = string.Format("(?<{0}>{1})", v_groupName, v_strKey);
                    var v_strValue = v_pair.Value == null ? "" : (v_pair.Value is string ? v_pair.Value as string : v_pair.Value.ToString());

                    v_groupNameToKeyIndex.Add(v_groupName, v_strKeys.Count);
                    v_strKeys.Add(v_strKey);
                    v_strValues.Add(v_strValue);
                }
            }

            if (v_groupNameToKeyIndex.Count == 0)
            {
                return p_stringSource;
            }

            //Join all string in one
            var v_pattern = String.Join("|", v_strKeys);
            var v_regex = new Regex(v_pattern);
#if NET_STANDARD_2_0
            var groupNames = v_regex.GetGroupNames();
#endif
            string v_replaced = v_regex.Replace(p_stringSource,
                m =>
                {
                    int v_index = -1;

                    string v_formattedValue = null;
                    List<string> v_params = new List<string>();

                    //Try find main match group
                    Group v_mainGroup = null;
#if NET_STANDARD_2_0
                    int groupCounter = 0;
#endif
                    foreach (Group v_group in m.Groups)
                    {
#if NET_STANDARD_2_0
                        var groupName = groupCounter < groupNames.Length? null : groupNames[groupCounter];
#else
                        var groupName = v_group.Name;
#endif
                        //var v_alreadyFoundMainMatch = v_formattedValue != null;
                        if ((v_group.Success /*|| v_alreadyFoundMainMatch*/) &&
                            groupName != null &&
                            v_groupNameToKeyIndex.TryGetValue(groupName, out v_index) &&
                            v_index >= 0 &&
                            v_index < v_strValues.Count)
                        {
                            //We found another group (we can skip search)
                            //if (v_alreadyFoundMainMatch)
                            //    break;
                            v_formattedValue = v_strValues[v_index];
                            v_mainGroup = v_group;
                            break;
                        }
                        //we Already found the main group so the next sucess matches must be the parameters inside this main group
                        /*else if (v_alreadyFoundMainMatch)
                        {
                            v_params.Add(v_group.Success? v_group.Value : "");
                        }*/
#if NET_STANDARD_2_0
                        groupCounter++;
#endif
                    }

                    //Now try find parameters in this main group (we can't search in same previous loop because collection doesn't preserve order)
                    if (v_mainGroup != null)
                    {
                        foreach (Group v_group in m.Groups)
                        {
                            if (v_group == v_mainGroup)
                                continue;
                            //parameter inside this main group
                            else
                            {
                                v_params.Add(v_group.Success ? v_group.Value : "");
                            }
                        }
                    }

                    //Not worked (matched in a subgroup?), skip replace
                    if (v_formattedValue == null)
                        v_formattedValue = m.ToString();
                    else if (v_params.Count > 0)
                        v_formattedValue = string.Format(v_formattedValue, v_params.ToArray());

                    return v_formattedValue;
                });

            return v_replaced;
        }

        #endregion
    }
}
