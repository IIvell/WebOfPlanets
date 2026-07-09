using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace xyz.germanfica.unity.planet.gravity
{
    public static class ReplaceCapsuleWithGrabBot
    {
        const string GrabBotAssetPath = "Assets/Prefabs/Character/Grab-Bot COMPLETE.fbx";

        [MenuItem("Tools/Replace Player Capsule With Grab-Bot")]
        static void Replace()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("Player GameObject nije pronađen u aktivnoj sceni.");
                return;
            }

            Transform capsule = player.transform.Find("Capsule");
            if (capsule == null)
            {
                Debug.LogError("Capsule child nije pronađen ispod Player.");
                return;
            }

            GameObject grabBotAsset = AssetDatabase.LoadAssetAtPath<GameObject>(GrabBotAssetPath);
            if (grabBotAsset == null)
            {
                Debug.LogError($"Grab-Bot COMPLETE.fbx nije pronađen na putanji: {GrabBotAssetPath}");
                return;
            }

            Vector3 localPos = capsule.localPosition;
            Quaternion localRot = capsule.localRotation;
            Vector3 localScale = capsule.localScale;
            string tag = capsule.gameObject.tag;
            int layer = capsule.gameObject.layer;

            GameObject grabBotInstance = (GameObject)PrefabUtility.InstantiatePrefab(grabBotAsset, player.transform);
            Undo.RegisterCreatedObjectUndo(grabBotInstance, "Replace Capsule with Grab-Bot");

            grabBotInstance.transform.localPosition = localPos;
            grabBotInstance.transform.localRotation = localRot;
            grabBotInstance.transform.localScale = localScale;
            grabBotInstance.tag = tag;
            grabBotInstance.layer = layer;

            Undo.DestroyObjectImmediate(capsule.gameObject);

            EditorSceneManager.MarkSceneDirty(player.scene);
            Debug.Log("Capsule zamijenjen s Grab-Bot COMPLETE ispod Player.");
        }

        [MenuItem("Tools/Center Grab-Bot Visual Pivot")]
        static void CenterPivot()
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("Player GameObject nije pronađen u aktivnoj sceni.");
                return;
            }

            PlayerController controller = player.GetComponent<PlayerController>();
            if (controller == null)
            {
                Debug.LogError("PlayerController nije pronađen na Player.");
                return;
            }

            SerializedObject so = new SerializedObject(controller);
            SerializedProperty visualModelProp = so.FindProperty("visualModel");
            Transform grabBot = visualModelProp.objectReferenceValue as Transform;
            if (grabBot == null)
                grabBot = player.transform.Find("Grab-Bot COMPLETE");

            if (grabBot == null)
            {
                Debug.LogError("Grab-Bot COMPLETE child nije pronađen ispod Player, niti povezan u Visual Model polju.");
                return;
            }

            if (grabBot.parent != player.transform)
            {
                Debug.LogWarning("Grab-Bot COMPLETE već ima drugog roditelja (pivot vjerojatno već postoji) - preskačem.");
                return;
            }

            Renderer[] renderers = grabBot.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                Debug.LogError("Nema Renderer komponenti ispod Grab-Bot COMPLETE - ne mogu izračunati pivot.");
                return;
            }

            Bounds bounds = renderers[0].bounds;
            foreach (Renderer r in renderers)
                bounds.Encapsulate(r.bounds);

            GameObject pivotGO = new GameObject("Grab-Bot Pivot");
            Undo.RegisterCreatedObjectUndo(pivotGO, "Center Grab-Bot Pivot");
            Transform pivot = pivotGO.transform;
            pivot.SetParent(player.transform, false);
            // Samo X/Z se centriraju na sredinu meša - to je horizontalna udaljenost od osi rotacije
            // (transform.up) koja uzrokuje "zamah" pri rotaciji. Y ostaje na visini modela.
            pivot.position = new Vector3(bounds.center.x, grabBot.position.y, bounds.center.z);
            pivot.rotation = grabBot.rotation;

            Undo.SetTransformParent(grabBot, pivot, "Center Grab-Bot Pivot");

            visualModelProp.objectReferenceValue = pivot;
            so.ApplyModifiedProperties();

            EditorSceneManager.MarkSceneDirty(player.scene);
            Debug.Log("Grab-Bot pivot centriran - PlayerController.visualModel sada pokazuje na Grab-Bot Pivot.");
        }

        [MenuItem("Tools/Grab-Bot Pivot/Raise 0.1m")]
        static void RaisePivot() => AdjustPivotHeight(0.1f);

        [MenuItem("Tools/Grab-Bot Pivot/Lower 0.1m")]
        static void LowerPivot() => AdjustPivotHeight(-0.1f);

        static void AdjustPivotHeight(float delta)
        {
            GameObject player = GameObject.Find("Player");
            if (player == null)
            {
                Debug.LogError("Player GameObject nije pronađen u aktivnoj sceni.");
                return;
            }

            Transform pivot = player.transform.Find("Grab-Bot Pivot");
            if (pivot == null)
            {
                Debug.LogError("Grab-Bot Pivot nije pronađen - prvo pokreni Center Grab-Bot Visual Pivot.");
                return;
            }

            // Lokalni Vector3.up jer je Player već poravnat s gravitacijom trenutnog planeta (Attractor),
            // pa lokalna Y os uvijek gleda "od planeta" bez obzira na kojem se planetu igrač nalazi.
            Undo.RecordObject(pivot, "Adjust Grab-Bot Pivot Height");
            pivot.localPosition += Vector3.up * delta;

            EditorSceneManager.MarkSceneDirty(player.scene);
            Debug.Log($"Grab-Bot Pivot lokalna Y sada: {pivot.localPosition.y:F3}");
        }
    }
}
