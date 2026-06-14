#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ARViewAlignmentCalibrator))]
public class ARViewAlignmentCalibratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ARViewAlignmentCalibrator calibrator =
            (ARViewAlignmentCalibrator)target;

        EditorGUILayout.Space();

        if (GUILayout.Button("Use Scene View Camera As Expected"))
        {
            SceneView sceneView = SceneView.lastActiveSceneView;
            if (sceneView == null || sceneView.camera == null)
            {
                Debug.LogWarning("No active Scene View camera found.");
                return;
            }

            if (calibrator.challenge != null)
            {
                Undo.RecordObject(calibrator.challenge, "Calibrate View Alignment");
            }

            calibrator.UseCameraViewAsExpected(sceneView.camera.transform);
            if (calibrator.challenge != null)
            {
                EditorUtility.SetDirty(calibrator.challenge);
            }
        }

        if (GUILayout.Button("Use Main Camera As Expected"))
        {
            calibrator.UseCurrentCameraViewAsExpected();
            if (calibrator.challenge != null)
            {
                EditorUtility.SetDirty(calibrator.challenge);
            }
        }

        if (GUILayout.Button("Log Current Alignment Angle"))
        {
            calibrator.LogCurrentAngle();
        }
    }
}
#endif
