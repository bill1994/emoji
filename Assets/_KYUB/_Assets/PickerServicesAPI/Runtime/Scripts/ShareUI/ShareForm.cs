using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Kyub;
using Kyub.UI;
using Kyub.Async;

namespace Kyub.PickerServices
{
    public class ShareForm : MonoBehaviour
    {
        #region Private Variables

        [SerializeField]
        string m_shareSubject = "";
        [SerializeField]
        string m_shareText = "";

        #endregion

        #region Public Properties

        public string ShareSubject
        {
            get
            {
                return m_shareSubject;
            }
            set
            {
                if (m_shareSubject == value)
                    return;
                m_shareSubject = value;
            }
        }

        public string ShareText
        {
            get
            {
                return m_shareText;
            }
            set
            {
                if (m_shareText == value)
                    return;
                m_shareText = value;
            }
        }

        #endregion

        #region Helper Functions

        public void ShareExternalImage(ExternalImage p_externalImage)
        {
            ShareFile(p_externalImage != null ? p_externalImage.Key : "");
        }

        public void ShareExternalImgFile(ExternImgFile p_externImgFile)
        {
            ShareFile(p_externImgFile != null ? p_externImgFile.Url : "");
        }

        public void ShareFile(string p_filePath)
        {
            if (!string.IsNullOrEmpty(p_filePath))
                CrossFileProvider.ShareFile(p_filePath, m_shareText, m_shareSubject);
        }

        public void ShareFiles(IEnumerable<string> p_filePaths)
        {
            if(p_filePaths != null)
                CrossFileProvider.ShareFiles(p_filePaths, m_shareText, m_shareSubject);
        }

        #endregion
    }
}
