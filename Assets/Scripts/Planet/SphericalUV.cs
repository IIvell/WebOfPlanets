using System.Collections.Generic;
using UnityEngine;

namespace xyz.germanfica.unity.planet.gravity
{
    // Runtime zamjena UV-ova mesha sfernim (equirect lat-long) mapiranjem iz same
    // geometrije. Hub FBX ima autorske UV otoke po kojima se proceduralna planetna
    // tekstura "resetirala" na granicama (vidljive crte) — sferni UV-ovi je polažu
    // kontinuirano, kao na primitivnim sferama spawnanih planeta. Radi na kopiji
    // mesha (asset se ne dira); collider koristi svoj mesh pa fizika ostaje ista.
    public static class SphericalUV
    {
        public static void Apply(Renderer renderer)
        {
            var meshFilter = renderer.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null) return;
            if (!meshFilter.sharedMesh.isReadable)
            {
                Debug.LogWarning($"SphericalUV: mesh '{meshFilter.sharedMesh.name}' nije Read/Write — preskačem.");
                return;
            }

            Mesh mesh = Object.Instantiate(meshFilter.sharedMesh);
            mesh.name = meshFilter.sharedMesh.name + " (spherical UV)";

            Vector3[] verts = mesh.vertices;
            Vector3 center = mesh.bounds.center;

            var vList  = new List<Vector3>(verts);
            var uvList = new List<Vector2>(verts.Length);
            var nList  = new List<Vector3>(mesh.normals);
            var tList  = new List<Vector4>();
            mesh.GetTangents(tList);

            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 d = (verts[i] - center).normalized;
                uvList.Add(new Vector2(
                    Mathf.Atan2(d.z, d.x) / (2f * Mathf.PI) + 0.5f,
                    Mathf.Asin(Mathf.Clamp(d.y, -1f, 1f)) / Mathf.PI + 0.5f));
            }

            // Trokuti koji premošćuju šav wrapa (u skače ~1 → ~0) razmazali bi
            // cijelu teksturu unatrag preko sebe — vrhove s niskim u dupliciramo
            // s u+1 (tekstura ima wrapModeU = Repeat pa u > 1 uredno sampla).
            var duplicated = new Dictionary<int, int>();
            var newTris = new int[mesh.subMeshCount][];

            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                int[] tris = mesh.GetTriangles(s);
                for (int t = 0; t < tris.Length; t += 3)
                {
                    float u0 = uvList[tris[t]].x, u1 = uvList[tris[t + 1]].x, u2 = uvList[tris[t + 2]].x;
                    if (Mathf.Max(u0, u1, u2) - Mathf.Min(u0, u1, u2) <= 0.5f) continue;

                    for (int k = 0; k < 3; k++)
                    {
                        int idx = tris[t + k];
                        if (uvList[idx].x >= 0.5f) continue;

                        if (!duplicated.TryGetValue(idx, out int dupIdx))
                        {
                            dupIdx = vList.Count;
                            vList.Add(vList[idx]);
                            uvList.Add(uvList[idx] + Vector2.right);
                            if (nList.Count > 0) nList.Add(nList[idx]);
                            if (tList.Count > 0) tList.Add(tList[idx]);
                            duplicated[idx] = dupIdx;
                        }
                        tris[t + k] = dupIdx;
                    }
                }
                newTris[s] = tris;
            }

            mesh.SetVertices(vList);
            mesh.SetUVs(0, uvList);
            if (nList.Count > 0) mesh.SetNormals(nList);
            if (tList.Count > 0) mesh.SetTangents(tList);
            for (int s = 0; s < newTris.Length; s++)
                mesh.SetTriangles(newTris[s], s, calculateBounds: false);

            meshFilter.sharedMesh = mesh;
        }
    }
}
