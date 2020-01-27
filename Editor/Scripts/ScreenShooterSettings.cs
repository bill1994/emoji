/*
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not
 * use this file except in compliance with the License. You may obtain a copy of
 * the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS, WITHOUT
 * WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the
 * License for the specific language governing permissions and limitations under
 * the License.
 */

#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using KyubEditor.ScreenShooter.Configs;
using KyubEditor.ScreenShooter.Utils;

namespace KyubEditor.ScreenShooter
{
    public class ScreenShooterSettings : ScriptableObject
    {
        private const string RELATIVE_PATH = "Editor/Data/ScreenShooterSettings.asset";

        public Camera Camera;
        public List<ScreenshotConfig> ScreenshotConfigs;
        public string Tag;
        public bool AppendTimestamp;
        public string SaveFolder;

        //---------------------------------------------------------------------
        // Public
        //---------------------------------------------------------------------

        public static ScreenShooterSettings Load()
        {
            return EditorUtil.LoadFromAsset<ScreenShooterSettings>(RELATIVE_PATH);
        }
    }
}

#endif