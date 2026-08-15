using System.Collections.Generic;
using UnityEngine;

namespace UpIzUpMini.EditorTools
{
    /// <summary>
    /// Vertex-clustering mesh decimation.
    ///
    /// Needed because the only "realistic" crop models available (from the
    /// larger Up Iz Up project, `Assets/Imported Plants/`) are ~2,000,000
    /// triangle photogrammetry scans in a single mesh - roughly 13x an
    /// entire mobile scene budget for ONE plant, before multiplying by six
    /// farm plots. Unity ships no built-in decimation, so rather than
    /// abandon the real plant shape and fall back to primitives, this
    /// reduces the mesh while preserving its silhouette.
    ///
    /// Algorithm: overlay a uniform grid on the mesh bounds, collapse every
    /// vertex in a cell to that cell's average position, then rebuild the
    /// triangle list and drop triangles whose corners collapsed into the
    /// same cell (degenerates). Simple, fast, topology-agnostic, and well
    /// suited to organic foliage where exact topology doesn't matter.
    /// Normals are recalculated afterwards.
    /// </summary>
    public static class MeshDecimator
    {
        public static Mesh Decimate(Mesh source, int gridResolution)
        {
            Vector3[] srcVerts = source.vertices;
            int[] srcTris = source.triangles;

            Bounds bounds = source.bounds;
            Vector3 size = bounds.size;
            float cell = Mathf.Max(size.x, Mathf.Max(size.y, size.z)) / Mathf.Max(2, gridResolution);
            if (cell <= 0f) return null;

            // Map every source vertex to a grid cell, accumulating an
            // average position per occupied cell.
            var cellToIndex = new Dictionary<long, int>(srcVerts.Length / 4);
            var accum = new List<Vector3>();
            var accumCount = new List<int>();
            var vertToNew = new int[srcVerts.Length];

            for (int i = 0; i < srcVerts.Length; i++)
            {
                Vector3 p = srcVerts[i];
                int cx = Mathf.FloorToInt((p.x - bounds.min.x) / cell);
                int cy = Mathf.FloorToInt((p.y - bounds.min.y) / cell);
                int cz = Mathf.FloorToInt((p.z - bounds.min.z) / cell);
                long key = ((long)cx << 42) ^ ((long)cy << 21) ^ (long)cz;

                if (!cellToIndex.TryGetValue(key, out int newIndex))
                {
                    newIndex = accum.Count;
                    cellToIndex[key] = newIndex;
                    accum.Add(Vector3.zero);
                    accumCount.Add(0);
                }

                accum[newIndex] += p;
                accumCount[newIndex]++;
                vertToNew[i] = newIndex;
            }

            var newVerts = new Vector3[accum.Count];
            for (int i = 0; i < accum.Count; i++)
            {
                newVerts[i] = accum[i] / Mathf.Max(1, accumCount[i]);
            }

            // Rebuild triangles, discarding degenerates (two or more corners
            // that landed in the same cell) and exact duplicates.
            var newTris = new List<int>(srcTris.Length / 4);
            var seenTri = new HashSet<long>();
            for (int t = 0; t < srcTris.Length; t += 3)
            {
                int a = vertToNew[srcTris[t]];
                int b = vertToNew[srcTris[t + 1]];
                int c = vertToNew[srcTris[t + 2]];
                if (a == b || b == c || a == c) continue;

                int lo = Mathf.Min(a, Mathf.Min(b, c));
                int hi = Mathf.Max(a, Mathf.Max(b, c));
                int mid = a + b + c - lo - hi;
                long triKey = ((long)lo << 42) ^ ((long)mid << 21) ^ (long)hi;
                if (!seenTri.Add(triKey)) continue;

                newTris.Add(a);
                newTris.Add(b);
                newTris.Add(c);
            }

            if (newTris.Count == 0) return null;

            var mesh = new Mesh { name = source.name + "_decimated" };
            mesh.indexFormat = newVerts.Length > 65000
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.vertices = newVerts;
            mesh.triangles = newTris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
