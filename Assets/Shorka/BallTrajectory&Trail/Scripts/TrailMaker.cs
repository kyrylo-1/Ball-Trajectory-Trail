using UnityEngine;
using System.Collections.Generic;

// Ball Trajectory & Trail
// Kyrylo Avramenko 
// https://github.com/kir-avramenko

namespace Shorka.BallTrajectory
{
    /// <summary>
    /// Control ball trail behaviour 
    /// </summary>
    public class TrailMaker : MonoBehaviour
    {
        public class AdvancedParameters
        {
            public int baseNbQuad = 500;
            public int nbQuadIncrement = 200;
            public int nbSegmentToParametrize = 3;
            public float lengthToRedraw = 0;
            public bool shiftMeshData = false;
        }

        public bool emit = true;

        public float width = 0.2f;
        public float height = 1f;
        private AdvancedParameters advancedParameters = new AdvancedParameters();

        [HideInInspector]
        public Spline spline;

        private const int NbVertexPerQuad = 4;
        private const int NbTriIndexPerQuad = 6;
        private Vector3 normal = new Vector3(0, 0, 1);
        private float emissionDistance = 0.1f;
        Vector3[] vertices;
        int[] triangles;
        Vector2[] uv;
        Vector3[] normals;

        private Vector3 origin;
        private int maxInstanciedTriCount = 0;
        private Mesh mesh;
        private int allocatedNbQuad;
        private int lastStartingQuad;
        private int quadOffset;

        public void Clear()
        {
            Init();
        }

        private void Awake()
        {
            spline = new Spline();
        }

        private void Start()
        {
            Init();
        }

        private void Update()
        {
            if (emit)
            {
                List<Node> nodes = spline.nodes;
                Vector3 point = transform.position;

                nodes[nodes.Count - 1].position = point;
                nodes[nodes.Count - 2].position = point;

                if (Vector3.Distance(nodes[nodes.Count - 3].position, point) > emissionDistance &&
                    Vector3.Distance(nodes[nodes.Count - 4].position, point) > emissionDistance)
                {
                    nodes.Add(new Node(point));
                }

            }

            if (spline != null)
            {
                RenderMesh();
            }

        }

        private void RenderMesh()
        {
            if (advancedParameters.nbSegmentToParametrize == 0)
                spline.Parametrize();
            else
            {
                spline.Parametrize(spline.NbSegments - advancedParameters.nbSegmentToParametrize, spline.NbSegments);
            }


            float length = Mathf.Max(spline.Length() - 0.1f, 0);

            int nbQuad = ((int)(1f / width * length)) + 1 - quadOffset;

            if (allocatedNbQuad < nbQuad) //allocate more memory for the mesh
            {
                Reallocate(nbQuad);
                length = Mathf.Max(spline.Length() - 0.1f, 0);
                nbQuad = ((int)(1f / width * length)) + 1 - quadOffset;
            }

            int startingQuad = lastStartingQuad;
            float lastDistance = startingQuad * width + quadOffset * width;
            maxInstanciedTriCount = System.Math.Max(maxInstanciedTriCount, (nbQuad - 1) * NbTriIndexPerQuad);

            Spline.Marker marker = new Spline.Marker();
            spline.PlaceMarker(marker, lastDistance);

            Vector3 lastPosition = spline.GetPosition(marker);
            Vector3 lastTangent = spline.GetTangent(marker);
            Vector3 lastBinormal = Spline.CalcBinormal(lastTangent, normal);

            int drawingEnd = nbQuad - 1;

            for (int i = startingQuad; i < drawingEnd; i++)
            {
                float distance = lastDistance + width;
                int firstVertexIndex = i * NbVertexPerQuad;
                int firstTriIndex = i * NbTriIndexPerQuad;

                spline.MoveMarker(marker, distance);

                Vector3 position = spline.GetPosition(marker);
                Vector3 tangent = spline.GetTangent(marker);
                Vector3 binormal = Spline.CalcBinormal(tangent, normal);

                float h = FadeMultiplier(lastDistance, length);
                float rh = h * height;

                rh = h > 0 ? height : 0;

                Vector3 pos = lastPosition + (lastTangent * width * -0.5f) - origin;

                vertices[firstVertexIndex] = transform.InverseTransformPoint(pos + (lastBinormal * (rh * 0.5f)));
                vertices[firstVertexIndex + 1] = transform.InverseTransformPoint(pos + (-lastBinormal * (rh * 0.5f)));
                vertices[firstVertexIndex + 2] = transform.InverseTransformPoint(pos + (lastTangent * width) + (lastBinormal * (rh * 0.5f)));
                vertices[firstVertexIndex + 3] = transform.InverseTransformPoint(pos + (lastTangent * width) + (-lastBinormal * (rh * 0.5f)));

                uv[firstVertexIndex] = new Vector2(0, 1);
                uv[firstVertexIndex + 1] = new Vector2(0, 0);
                uv[firstVertexIndex + 2] = new Vector2(1, 1);
                uv[firstVertexIndex + 3] = new Vector2(1, 0);

                triangles[firstTriIndex] = firstVertexIndex;
                triangles[firstTriIndex + 1] = firstVertexIndex + 1;
                triangles[firstTriIndex + 2] = firstVertexIndex + 2;
                triangles[firstTriIndex + 3] = firstVertexIndex + 2;
                triangles[firstTriIndex + 4] = firstVertexIndex + 1;
                triangles[firstTriIndex + 5] = firstVertexIndex + 3;

                lastPosition = position;
                lastTangent = tangent;
                lastBinormal = binormal;
                lastDistance = distance;
            }

            for (int i = (nbQuad - 1) * NbTriIndexPerQuad; i < maxInstanciedTriCount; i++) //clear a few tri ahead
                triangles[i] = 0;

            lastStartingQuad = advancedParameters.lengthToRedraw == 0 ?
                System.Math.Max(0, nbQuad - ((int)(100 / width) + 5)) :
                System.Math.Max(0, nbQuad - ((int)(advancedParameters.lengthToRedraw / width) + 5));

            mesh.Clear();
            mesh.vertices = vertices;
            mesh.uv = uv;
            mesh.triangles = triangles;

        }

        private void Init()
        {
            origin = Vector3.zero;

            if (mesh == null)
            {
                mesh = GetComponent<MeshFilter>().mesh;
                mesh.MarkDynamic();
            }


            allocatedNbQuad = advancedParameters.baseNbQuad;
            maxInstanciedTriCount = 0;
            lastStartingQuad = 0;
            quadOffset = 0;

            vertices = new Vector3[advancedParameters.baseNbQuad * NbVertexPerQuad];
            triangles = new int[advancedParameters.baseNbQuad * NbTriIndexPerQuad];
            uv = new Vector2[advancedParameters.baseNbQuad * NbVertexPerQuad];
            normals = new Vector3[advancedParameters.baseNbQuad * NbVertexPerQuad];

            if (normal == Vector3.zero)
                normal = (transform.position - Camera.main.transform.position).normalized;

            for (int i = 0; i < normals.Length; i++)
                normals[i] = normal;

            spline.Clear();

            Vector3 point = transform.position;
            List<Node> nodes = spline.nodes;

            for (int i = 0; i < 5; i++)
                nodes.Add(new Node(point));
        }

        private void Reallocate(int nbQuad)
        {
            if (advancedParameters.shiftMeshData && lastStartingQuad > 0/*advancedParameters.nbQuadIncrement / 4*/) //slide
            {
                int newIndex = 0;
                for (int i = lastStartingQuad; i < nbQuad; i++)
                {
                    vertices[newIndex] = vertices[i];
                    triangles[newIndex] = triangles[i];
                    uv[newIndex] = uv[i];
                    normals[newIndex] = normals[i];
                    newIndex++;
                }

                quadOffset += lastStartingQuad;
                lastStartingQuad = 0;
            }

            if (allocatedNbQuad < nbQuad - quadOffset)
            {
                if ((allocatedNbQuad + advancedParameters.nbQuadIncrement) * NbVertexPerQuad > 65000)
                {
                    Clear();
                    return;
                }

                allocatedNbQuad += advancedParameters.nbQuadIncrement;

                Vector3[] vertices2 = new Vector3[allocatedNbQuad * NbVertexPerQuad];
                int[] triangles2 = new int[allocatedNbQuad * NbTriIndexPerQuad];
                Vector2[] uv2 = new Vector2[allocatedNbQuad * NbVertexPerQuad];
                Vector3[] normals2 = new Vector3[allocatedNbQuad * NbVertexPerQuad];

                vertices.CopyTo(vertices2, 0);
                triangles.CopyTo(triangles2, 0);
                uv.CopyTo(uv2, 0);
                normals.CopyTo(normals2, 0);

                vertices = vertices2;
                triangles = triangles2;
                uv = uv2;
                normals = normals2;

            }
        }

        float FadeMultiplier(float distance, float length)
        {
            float ha = Mathf.Clamp01((distance - Mathf.Max(length - 100, 0)) / 5);
            float hb = Mathf.Clamp01((length - distance) / 5);

            return Mathf.Min(ha, hb);
        }
    }
}