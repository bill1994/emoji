using System.IO;
using UnityEditor;
using UnityEngine;
#if UNITY_IOS
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
#endif

namespace NativeCameraNamespace
{
	public class NCPostProcessBuild
	{
		private const bool ENABLED = true;

		private const string CAMERA_USAGE_DESCRIPTION = "The app requires access to the camera to take pictures or record videos with it.";
		private const string MICROPHONE_USAGE_DESCRIPTION = "The app will capture microphone input in the recorded video.";

		[InitializeOnLoadMethod]
		public static void ValidatePlugin()
		{
			string jarPath = "Assets/Plugins/NativeCamera/Android/NativeCamera.jar";
			if( File.Exists( jarPath ) )
			{
				Debug.Log( "Deleting obsolete " + jarPath );
				AssetDatabase.DeleteAsset( jarPath );
			}
		}

#if UNITY_IOS
#pragma warning disable 0162
		[PostProcessBuild]
		public static void OnPostprocessBuild( BuildTarget target, string buildPath )
		{
			if( !ENABLED )
				return;

			if( target == BuildTarget.iOS )
			{
				string pbxProjectPath = PBXProject.GetPBXProjectPath( buildPath );
				string plistPath = Path.Combine( buildPath, "Info.plist" );

				PBXProject pbxProject = new PBXProject();
				pbxProject.ReadFromFile( pbxProjectPath );

#if UNITY_2019_3_OR_NEWER
				string targetGUID = pbxProject.GetUnityFrameworkTargetGuid();
#else
				string targetGUID = pbxProject.TargetGuidByName( PBXProject.GetUnityTargetName() );
#endif

				pbxProject.AddBuildProperty( targetGUID, "OTHER_LDFLAGS", "-framework MobileCoreServices" );
				pbxProject.AddBuildProperty( targetGUID, "OTHER_LDFLAGS", "-framework ImageIO" );

				File.WriteAllText( pbxProjectPath, pbxProject.WriteToString() );

				PlistDocument plist = new PlistDocument();
				plist.ReadFromString( File.ReadAllText( plistPath ) );

				string cameraUsage = !string.IsNullOrEmpty(PlayerSettings.iOS.cameraUsageDescription)? 
					PlayerSettings.iOS.cameraUsageDescription :
					CAMERA_USAGE_DESCRIPTION;
				string micUsage = !string.IsNullOrEmpty(PlayerSettings.iOS.microphoneUsageDescription) ?
					PlayerSettings.iOS.microphoneUsageDescription :
					MICROPHONE_USAGE_DESCRIPTION;

				PlistElementDict rootDict = plist.root;
				rootDict.SetString( "NSCameraUsageDescription", cameraUsage );
				rootDict.SetString( "NSMicrophoneUsageDescription", micUsage );

				File.WriteAllText( plistPath, plist.WriteToString() );
			}
		}
#pragma warning restore 0162
#endif
	}
}