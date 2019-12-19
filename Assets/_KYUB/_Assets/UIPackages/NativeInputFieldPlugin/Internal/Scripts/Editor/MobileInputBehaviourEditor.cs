using UnityEditor;
using UnityEngine;

namespace MobileInputNativePlugin {

    [CustomEditor (typeof (MobileInputBehaviour))]
    public class MobileInputBehaviourEditor : Editor {

        const int OFFSET = 20;

        const int SPACE = 5;

        private MobileInputBehaviour _target;

        private SerializedObject _object;

        private SerializedProperty _onReturnPressedEvent;

        private void OnEnable () {
            _target = (MobileInputBehaviour) target;
            _object = new SerializedObject (target);
            _onReturnPressedEvent = _object.FindProperty ("OnReturnPressedEvent");
        }

        public override void OnInspectorGUI () {
            _object.Update ();
            EditorGUI.BeginChangeCheck ();
            GUILayout.Space (OFFSET);
            GUILayout.Label ("Select type for Return button:");
            _target.ReturnKey = (MobileInputBehaviour.ReturnKeyType) GUILayout.Toolbar ((int) _target.ReturnKey, new string[] { "Default", "Next", "Done", "Search" });
            GUILayout.Space (OFFSET);

            GUILayout.Label ("Options:");
            _target.IsWithDoneButton = GUILayout.Toggle (_target.IsWithDoneButton, " Show \"Done\" button");
            GUILayout.Space (SPACE);
            _target.IsWithClearButton = GUILayout.Toggle (_target.IsWithClearButton, " Show \"Clear\" button");
            GUILayout.Space (OFFSET);

            EditorGUILayout.PropertyField (_onReturnPressedEvent);
            if (EditorGUI.EndChangeCheck ()) {
                _object.ApplyModifiedProperties ();
            }
        }

    }
}