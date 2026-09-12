using UnityEngine;
namespace UpIzUpMini.Character
{
    // Mesh swaps happen while the wardrobe pauses gameplay. Read the current
    // bone matrices directly so its portrait never waits for a GPU skinning frame.
    public static class WardrobePreviewMesh
    {
        public static Mesh Bake(SkinnedMeshRenderer renderer)
        {
            var source=renderer.sharedMesh;
            if(!source.isReadable){var fallback=new Mesh();renderer.BakeMesh(fallback);return fallback;}
            var result=Object.Instantiate(source);
            var vertices=source.vertices;var normals=source.normals;var weights=source.boneWeights;
            var binds=source.bindposes;var bones=renderer.bones;
            var matrices=new Matrix4x4[binds.Length];var normalMatrices=new Matrix4x4[binds.Length];
            for(int i=0;i<binds.Length;i++){matrices[i]=renderer.transform.worldToLocalMatrix*bones[i].localToWorldMatrix*binds[i];normalMatrices[i]=matrices[i].inverse.transpose;}
            for(int i=0;i<vertices.Length;i++)
            {
                var w=weights[i];var p=vertices[i];var n=normals[i];
                vertices[i]=matrices[w.boneIndex0].MultiplyPoint3x4(p)*w.weight0+matrices[w.boneIndex1].MultiplyPoint3x4(p)*w.weight1+matrices[w.boneIndex2].MultiplyPoint3x4(p)*w.weight2+matrices[w.boneIndex3].MultiplyPoint3x4(p)*w.weight3;
                normals[i]=(normalMatrices[w.boneIndex0].MultiplyVector(n)*w.weight0+normalMatrices[w.boneIndex1].MultiplyVector(n)*w.weight1+normalMatrices[w.boneIndex2].MultiplyVector(n)*w.weight2+normalMatrices[w.boneIndex3].MultiplyVector(n)*w.weight3).normalized;
            }
            result.vertices=vertices;result.normals=normals;result.RecalculateBounds();return result;
        }
    }
}
