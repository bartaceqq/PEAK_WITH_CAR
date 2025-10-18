#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class RoadBuilderPro : EditorWindow
{
    [System.Serializable]
    public class RoadList : ScriptableObject
    {
        public List<GameObject> prefabs = new();
        public int selectedIndex = 0;
    }

    RoadList data;
    bool placeMode = false;

    GameObject previewGO;
    RoadModule previewModule;

    float snapDistance = 2.0f;
    float groundHeight = 0f;

    bool hasSnap;
    Vector3 snapPos;
    Quaternion snapRot;

    Transform targetSocketTf;
    SocketType targetSocketType;

    Material ghostMat;

    [MenuItem("Tools/Road Builder Pro")]
    public static void Open() => GetWindow<RoadBuilderPro>("Road Builder Pro");

    void OnEnable()
    {
        if (data == null) data = CreateInstance<RoadList>();
        SceneView.duringSceneGui += OnSceneGUI;

        // Ghost material (safe fallback if shader missing)
        var sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (sh != null)
        {
            ghostMat = new Material(sh);
            ghostMat.SetFloat("_Surface", 1); // transparent if URP
            var c = new Color(0.1f, 1f, 0.4f, 0.35f);
            if (ghostMat.HasProperty("_BaseColor")) ghostMat.SetColor("_BaseColor", c);
            if (ghostMat.HasProperty("_Color"))     ghostMat.SetColor("_Color", c);
            if (ghostMat.HasProperty("_Blend"))     ghostMat.SetFloat("_Blend", 1);
            if (ghostMat.HasProperty("_ZWrite"))    ghostMat.SetFloat("_ZWrite", 0);
        }
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
        DestroyPreview();
        if (ghostMat) DestroyImmediate(ghostMat);
    }

    void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Prefabs", EditorStyles.boldLabel);

        int removeIdx = -1;
        for (int i = 0; i < data.prefabs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            data.prefabs[i] = (GameObject)EditorGUILayout.ObjectField(data.prefabs[i], typeof(GameObject), false);
            if (GUILayout.Toggle(data.selectedIndex == i, "Use", "Button", GUILayout.Width(54)))
                data.selectedIndex = i;
            if (GUILayout.Button("X", GUILayout.Width(24))) removeIdx = i;
            EditorGUILayout.EndHorizontal();
        }
        if (removeIdx >= 0)
        {
            data.prefabs.RemoveAt(removeIdx);
            if (data.selectedIndex >= data.prefabs.Count) data.selectedIndex = data.prefabs.Count - 1;
        }

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("+ Add Prefab", GUILayout.Width(140)))
            data.prefabs.Add(null);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        snapDistance = EditorGUILayout.Slider(new GUIContent("Snap Distance"), snapDistance, 0.2f, 5f);
        groundHeight = EditorGUILayout.FloatField(new GUIContent("Default Ground Y"), groundHeight);

        EditorGUILayout.Space();
        bool canPlace = data.prefabs.Count > 0 && data.selectedIndex >= 0 &&
                        data.selectedIndex < data.prefabs.Count && data.prefabs[data.selectedIndex] != null;

        EditorGUI.BeginDisabledGroup(!canPlace);
        string btn = placeMode ? "Stop Place Mode" : "Start Place Mode";
        if (GUILayout.Button(btn, GUILayout.Height(28)))
        {
            placeMode = !placeMode;
            if (placeMode) CreatePreview();
            else DestroyPreview();
            SceneView.RepaintAll();
        }
        EditorGUI.EndDisabledGroup();

        if (placeMode)
        {
            EditorGUILayout.HelpBox("Left Click: place piece\nRight Click / Esc: cancel place mode\nMouse Move: move preview\nSnap occurs near existing sockets", MessageType.Info);
        }
    }

    void CreatePreview()
    {
        DestroyPreview();
        var prefab = data.prefabs[data.selectedIndex];
        if (!prefab)
            return;

        previewGO = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        previewGO.name = "[Preview] " + prefab.name;
        previewGO.hideFlags = HideFlags.HideAndDontSave;

        // disable colliders & physics on preview
        foreach (var c in previewGO.GetComponentsInChildren<Collider>()) c.enabled = false;
        foreach (var rb in previewGO.GetComponentsInChildren<Rigidbody>()) rb.isKinematic = true;

        // ghost look
        if (ghostMat)
        {
            foreach (var r in previewGO.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++) mats[i] = ghostMat;
                r.sharedMaterials = mats;
            }
        }

        previewModule = previewGO.GetComponent<RoadModule>();
        if (!previewModule)
            Debug.LogWarning("Selected prefab has no RoadModule component. Please add it and set sockets.");
    }

    void DestroyPreview()
    {
        if (previewGO) DestroyImmediate(previewGO);
        previewGO = null;
        previewModule = null;
    }

    void OnSceneGUI(SceneView sv)
    {
        if (!placeMode) return;

        var e = Event.current;

        // scene ray from mouse
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        // move preview (hit geometry or ground plane)
        if (Physics.Raycast(ray, out var hit, 10000f))
            UpdatePreviewAt(hit.point);
        else
        {
            var plane = new Plane(Vector3.up, new Vector3(0, groundHeight, 0));
            if (plane.Raycast(ray, out float d))
                UpdatePreviewAt(ray.GetPoint(d));
        }

        // draw snap highlight
        if (hasSnap)
            DrawGreenSnapSquare(snapPos, snapRot, 0.9f);

        // place
        if (e.type == EventType.MouseDown && e.button == 0 && GUIUtility.hotControl == 0)
        {
            TryPlace();
            e.Use();
        }

        // cancel
        if ((e.type == EventType.MouseDown && e.button == 1) ||
            (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape))
        {
            placeMode = false;
            DestroyPreview();
            Repaint();
            e.Use();
        }
    }

    void UpdatePreviewAt(Vector3 cursorWorld)
    {
        if (!previewGO) CreatePreview();
        if (!previewGO) return;

        // find nearest socket in the scene (from all RoadModules)
        (Transform tf, SocketType type, float dist) best = (null, SocketType.Forward, float.MaxValue);

        foreach (var mod in GameObject.FindObjectsByType<RoadModule>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (previewGO && mod.gameObject == previewGO) continue; // skip preview
            foreach (var pair in mod.AllSockets())
            {
                Transform tf = pair.tf;
                if (!tf) continue;
                float d = Vector3.Distance(cursorWorld, tf.position);
                if (d < best.dist)
                    best = (tf, pair.type, d);
            }
        }

        hasSnap = (best.tf && best.dist <= snapDistance && previewModule);

        if (hasSnap)
        {
            // Align our opposite socket onto the target socket
            var myType   = RoadModule.Opposite(best.type);
            var mySocket = previewModule.GetSocket(myType);

            if (!mySocket)
            {
                previewGO.transform.SetPositionAndRotation(best.tf.position, best.tf.rotation);
            }
            else
            {
                // robust orientation match: use socket forward/up
                Quaternion myLocalOri  = Quaternion.LookRotation(mySocket.forward, mySocket.up);
                Quaternion tgtWorldOri = Quaternion.LookRotation(best.tf.forward, best.tf.up);

                Quaternion rot = tgtWorldOri * Quaternion.Inverse(myLocalOri);
                Vector3 pos    = best.tf.position - rot * mySocket.localPosition;

                previewGO.transform.SetPositionAndRotation(pos, rot);
            }

            snapPos = best.tf.position;
            snapRot = best.tf.rotation;
            targetSocketTf = best.tf;
            targetSocketType = best.type;
        }
        else
        {
            // free placement on ground plane at cursor
            var pos = new Vector3(cursorWorld.x, groundHeight, cursorWorld.z);

            // face scene view camera direction (Y only)
            var view = SceneView.lastActiveSceneView ? SceneView.lastActiveSceneView.camera.transform.forward : Vector3.forward;
            view.y = 0; view.Normalize();
            if (view.sqrMagnitude < 0.01f) view = Vector3.forward;

            previewGO.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(view, Vector3.up));
            targetSocketTf = null;
        }

        SceneView.RepaintAll();
    }

    void TryPlace()
    {
        if (!previewGO) return;

        // instantiate a real copy at preview transform
        var prefab = data.prefabs[data.selectedIndex];
        if (!prefab) return;

        GameObject real = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        Undo.RegisterCreatedObjectUndo(real, "Place Road Piece");
        real.transform.SetPositionAndRotation(previewGO.transform.position, previewGO.transform.rotation);

        // ensure the placed piece has RoadModule (so future snaps can find it)
        if (!real.GetComponent<RoadModule>())
            Debug.LogWarning("Placed prefab has no RoadModule component. Add it to keep snapping working.");
    }

    // visual green square indicating snap location & rotation
    void DrawGreenSnapSquare(Vector3 center, Quaternion rot, float size)
    {
        Vector3 f = rot * Vector3.forward;
        Vector3 r = rot * Vector3.right;

        Vector3 p0 = center - r * size * 0.5f - f * size * 0.5f;
        Vector3 p1 = center + r * size * 0.5f - f * size * 0.5f;
        Vector3 p2 = center + r * size * 0.5f + f * size * 0.5f;
        Vector3 p3 = center - r * size * 0.5f + f * size * 0.5f;

        Handles.DrawSolidRectangleWithOutline(new[] { p0, p1, p2, p3 },
            new Color(0f, 1f, 0f, 0.25f), new Color(0f, 1f, 0f, 0.95f));
    }
}
#endif
