using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MaterialUI
{
    #region Helper Classes

    [System.Serializable]
    public class StyleData
    {
        #region Private Variables

        [SerializeField]
        string m_name = null;
        [SerializeField]
        GameObject m_assetPrefab = null;

        #endregion

        #region Public Properties

        public string Name
        {
            get
            {
                return m_name;
            }

            set
            {
                m_name = value;
            }
        }

        public BaseStyleElement Asset
        {
            get
            {
                return m_assetPrefab != null? m_assetPrefab.GetComponent<BaseStyleElement>() : null;
            }

            set
            {
                m_assetPrefab = value != null? value.gameObject : null;
            }
        }

        #endregion

        #region Constructors

        public StyleData()
        {
        }

        public StyleData(string p_name, BaseStyleElement p_asset)
        {
            m_name = p_name;
            m_assetPrefab = p_asset != null? p_asset.gameObject : null;
        }

        #endregion
    }

    #endregion

    [CreateAssetMenu]
    public class StyleSheetAsset : ScriptableObject
    {
        #region Private Variables

        [SerializeField]
        List<StyleData> m_styles = new List<StyleData>();

        #endregion

        #region Public Properties

        public Dictionary<string, StyleData> StyleObjectsMap
        {
            get
            {
                if (_styleDataMap == null)
                    Optimize();
                return _styleDataMap;
            }
        }

        #endregion

        #region Unity Functions

        protected virtual void Awake()
        {
            Optimize();
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            Optimize();
        }
#endif

        #endregion

        #region Helper Functions

        public virtual List<StyleData> GetAllStyleDatasFromType(System.Type p_type)
        {
            List<StyleData> v_allValidStyleBehaviours = new List<StyleData>();
            foreach (var v_style in this.StyleObjectsMap)
            {
                if (v_style.Value != null)
                {
                    var v_styleValueType = v_style.Value.Asset != null? v_style.Value.Asset.GetType() : null;
                    if (v_styleValueType == null || p_type == v_styleValueType || v_styleValueType.IsSubclassOf(p_type))
                        v_allValidStyleBehaviours.Add(v_style.Value);
                }
            }
            return v_allValidStyleBehaviours;
        }

        public bool TryGetStyleDataOrFirstValid(string p_name, System.Type p_acceptedType, out StyleData p_style)
        {
            TryGetStyleData(p_name, p_acceptedType, out p_style);

            var v_type = p_style != null ? p_style.Asset.GetType() : null;
            if (p_acceptedType != null && (p_style == null || v_type != null))
            {
                //Find First Valid of the type
                if (p_style == null || (p_acceptedType != v_type && !v_type.IsSubclassOf(p_acceptedType)))
                {
                    p_style = null;
                    foreach (var v_style in m_styles)
                    {
                        if (v_style != null)
                        {
                            var v_styleValueType = v_style.Asset != null? v_style.Asset.GetType() : null;
                            if (v_styleValueType == null || (p_acceptedType == v_styleValueType || v_styleValueType.IsSubclassOf(p_acceptedType)))
                            {
                                p_style = v_style;
                                break;
                            }
                        }
                    }
                }
            }
            return p_style != null;
        }

        public bool TryGetStyleData(string p_name, out StyleData p_style)
        {
            return TryGetStyleData(p_name, null, out p_style);
        }

        public bool TryGetStyleData(string p_name, System.Type p_acceptedType, out StyleData p_style)
        {
            StyleObjectsMap.TryGetValue(p_name, out p_style);

            if (p_acceptedType != null && p_style != null && p_style.Asset != null)
            {
                var v_type = p_style.Asset.GetType();
                if (p_acceptedType != v_type && !v_type.IsSubclassOf(p_acceptedType))
                    p_style = null;
            }
            return p_style != null;
        }

        Dictionary<string, StyleData> _styleDataMap = null;
        public virtual void Optimize()
        {
            if (_styleDataMap == null)
                _styleDataMap = new Dictionary<string, StyleData>();
            else
                _styleDataMap.Clear();
            foreach (var v_style in m_styles)
            {
                if(v_style != null && v_style.Asset != null && !string.IsNullOrEmpty(v_style.Name) && !_styleDataMap.ContainsKey(v_style.Name))
                    _styleDataMap.Add(v_style.Name, v_style);
            }
        }

        #endregion
    }
}
