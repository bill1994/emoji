using UnityEngine;
using System.Collections;

namespace Kyub
{
    [System.Serializable]
    public abstract class FoldOutStruct
    {
        #region Inspector Properties

        #if !UNITY_EDITOR
        [System.Xml.Serialization.XmlIgnore]
        #endif
        [SerializeField, HideInInspector]
        bool _foldOut = true;

        #if !UNITY_EDITOR
        [System.Xml.Serialization.XmlIgnore]
        #endif
        public bool FoldOut { get { return _foldOut; } set { _foldOut = value; } }

        #endregion
    }
}
