using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Ball Trajectory & Trail
// Kyrylo Avramenko 
// https://github.com/kir-avramenko
namespace Shorka.BallTrajectory
{
    [System.Serializable]
    public class Node
    {
        public float distanceFromStart = -0.3f;
        public Spline.subNode[] subNodes = new Spline.subNode[Spline.NbSubSegmentPerSegment + 1]; //[0, 1]
        public Vector3 position;

        public Node(Vector3 position)
        {
            this.position = position;
        }

        public void Invalidate()
        {
            distanceFromStart = -0.3f;
        }
    }

    public class Spline
    {
        public struct subNode
        {
            public float distanceFromStart;
            public Vector3 position;
            public Vector3 tangent;
        }

        public class Marker
        {
            public int segmentIndex;
            public int subKnotAIndex;
            public int subKnotBIndex;
            public float lerpRatio;
        }

        public List<Node> nodes = new List<Node>();

        public const int NbSubSegmentPerSegment = 10;

        private const int MinimumKnotNb = 4;
        private const int FirstSegmentKnotIndex = 2;
        private readonly Vector3 vec3 = Vector3.zero;

        public int NbSegments { get { return System.Math.Max(0, nodes.Count - 3); } }

        public Vector3 FindPositionFromDistance(float distance)
        {
            Vector3 tangent = vec3;

            Marker result = new Marker();
            bool foundSegment = PlaceMarker(result, distance);

            if (foundSegment)
            {
                tangent = GetPosition(result);
            }

            return tangent;
        }

        public Vector3 FindTangentFromDistance(float distance)
        {
            Vector3 tangent = vec3;

            Marker result = new Marker();
            bool foundSegment = PlaceMarker(result, distance);

            if (foundSegment) tangent = GetTangent(result);
            return tangent;

        }

        public static Vector3 CalcBinormal(Vector3 tangent, Vector3 normal)
        {
            return Vector3.Cross(tangent, normal).normalized;
        }

        public float Length()
        {
            if (NbSegments == 0) return 0f;
            return System.Math.Max(0, GetSegmentDistanceFromStart(NbSegments - 1));
        }

        public void Clear()
        {
            nodes.Clear();
        }

        public void MoveMarker(Marker marker, float distance) //in Unity units
        {
            PlaceMarker(marker, distance, marker);
        }

        public Vector3 GetPosition(Marker marker)
        {
            Vector3 pos = vec3;

            if (NbSegments == 0) return pos;

            subNode[] subKnots = GetSegmentSubKnots(marker.segmentIndex);

            pos = Vector3.Lerp(subKnots[marker.subKnotAIndex].position,
                subKnots[marker.subKnotBIndex].position, marker.lerpRatio);

            return pos;
        }

        public Vector3 GetTangent(Marker marker)
        {
            Vector3 tangent = vec3;

            if (NbSegments == 0) return tangent;

            subNode[] subNodes = GetSegmentSubKnots(marker.segmentIndex);

            tangent = Vector3.Lerp(subNodes[marker.subKnotAIndex].tangent,
                subNodes[marker.subKnotBIndex].tangent, marker.lerpRatio);

            return tangent;
        }

        private float Epsilon { get { return 1f / NbSubSegmentPerSegment; } }

        private subNode[] GetSegmentSubKnots(int i)
        {
            return nodes[FirstSegmentKnotIndex + i].subNodes;
        }

        public float GetSegmentDistanceFromStart(int i)
        {
            return nodes[FirstSegmentKnotIndex + i].distanceFromStart;
        }

        private bool IsSegmentValid(int i)
        {
            return nodes[i].distanceFromStart != -1f && nodes[i + 1].distanceFromStart != -1f &&
                nodes[i + 2].distanceFromStart != -1f && nodes[i + 3].distanceFromStart != -1f;
        }

        private bool OutOfBoundSegmentIndex(int i)
        {
            return i < 0 || i >= NbSegments;
        }

        public void Parametrize()
        {
            Parametrize(0, NbSegments - 1);
        }

        public void Parametrize(int fromSegmentIndex, int toSegmentIndex)
        {
            if (nodes.Count < MinimumKnotNb) return;

            int nbSegments = System.Math.Min(toSegmentIndex + 1, NbSegments);
            fromSegmentIndex = System.Math.Max(0, fromSegmentIndex);
            float totalDistance = 0;

            if (fromSegmentIndex > 0)
            {
                totalDistance = GetSegmentDistanceFromStart(fromSegmentIndex - 1);
            }

            for (int i = fromSegmentIndex; i < nbSegments; i++)
            {
                subNode[] subKnots = GetSegmentSubKnots(i);
                for (int j = 0; j < subKnots.Length; j++)
                {
                    subNode sk = new subNode();

                    sk.distanceFromStart = totalDistance += CalcSegmentLen(i, (j - 1) * Epsilon, j * Epsilon);
                    sk.position = GetPositionOnSegment(i, j * Epsilon);
                    sk.tangent = GetTangentOnSegment(i, j * Epsilon);

                    subKnots[j] = sk;
                }

                nodes[FirstSegmentKnotIndex + i].distanceFromStart = totalDistance;
            }
        }

        public bool PlaceMarker(Marker result, float distance, Marker from = null)
        {
            subNode[] subNodes;
            int nbSegments = NbSegments;

            if (nbSegments == 0) return false;


            if (distance <= 0)
            {
                result.segmentIndex = 0;
                result.subKnotAIndex = 0;
                result.subKnotBIndex = 1;
                result.lerpRatio = 0f;
                return true;
            }
            else if (distance >= Length())
            {
                subNodes = GetSegmentSubKnots(nbSegments - 1);
                result.segmentIndex = nbSegments - 1;
                result.subKnotAIndex = subNodes.Length - 2;
                result.subKnotBIndex = subNodes.Length - 1;
                result.lerpRatio = 1f;
                return true;
            }

            int fromSegmentIndex = 0;
            int fromSubKnotIndex = 1;
            if (from != null)
            {
                fromSegmentIndex = from.segmentIndex;
            }

            for (int i = fromSegmentIndex; i < nbSegments; i++)
            {
                if (distance > GetSegmentDistanceFromStart(i)) continue;

                subNodes = GetSegmentSubKnots(i);

                for (int j = fromSubKnotIndex; j < subNodes.Length; j++)
                {
                    subNode sk = subNodes[j];

                    if (distance > sk.distanceFromStart) continue;

                    result.segmentIndex = i;
                    result.subKnotAIndex = j - 1;
                    result.subKnotBIndex = j;
                    result.lerpRatio = 1f - ((sk.distanceFromStart - distance) /
                        (sk.distanceFromStart - subNodes[j - 1].distanceFromStart));

                    break;
                }

                break;
            }

            return true;
        }


        private float CalcSegmentLen(int segmentIndex, float from, float to)
        {
            //Debug.Log("<color=red>ComputeLengthOfSegment:</color>");
            float length = 0;
            from = Mathf.Clamp01(from);
            to = Mathf.Clamp01(to);

            Vector3 lastPoint = GetPositionOnSegment(segmentIndex, from);

            for (float j = from + Epsilon; j < to + Epsilon / 2f; j += Epsilon)
            {
                Vector3 point = GetPositionOnSegment(segmentIndex, j);
                length += Vector3.Distance(point, lastPoint);
                lastPoint = point;
            }

            return length;
        }

        private Vector3 GetPositionOnSegment(int segmentIndex, float t)
        {
            return FindSplinePoint(nodes[segmentIndex].position, nodes[segmentIndex + 1].position,
                nodes[segmentIndex + 2].position, nodes[segmentIndex + 3].position, t);
        }

        private Vector3 GetTangentOnSegment(int segmentIndex, float t)
        {
            return FindSplineTangent(nodes[segmentIndex].position, nodes[segmentIndex + 1].position,
                nodes[segmentIndex + 2].position, nodes[segmentIndex + 3].position, t).normalized;
        }

        private static Vector3 FindSplinePoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            Vector3 ret = new Vector3();

            float t2 = t * t;
            float t3 = t2 * t;

            ret.x = 0.5f * ((2.0f * p1.x) +
                (-p0.x + p2.x) * t +
                (2.0f * p0.x - 5.0f * p1.x + 4 * p2.x - p3.x) * t2 +
                (-p0.x + 3.0f * p1.x - 3.0f * p2.x + p3.x) * t3);

            ret.y = 0.5f * ((2.0f * p1.y) +
                (-p0.y + p2.y) * t +
                (2.0f * p0.y - 5.0f * p1.y + 4 * p2.y - p3.y) * t2 +
                (-p0.y + 3.0f * p1.y - 3.0f * p2.y + p3.y) * t3);

            ret.z = 0.5f * ((2.0f * p1.z) +
                (-p0.z + p2.z) * t +
                (2.0f * p0.z - 5.0f * p1.z + 4 * p2.z - p3.z) * t2 +
                (-p0.z + 3.0f * p1.z - 3.0f * p2.z + p3.z) * t3);

            return ret;
        }

        private static Vector3 FindSplineTangent(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            Vector3 ret = new Vector3();

            float t2 = t * t;

            ret.x = 0.5f * (-p0.x + p2.x) +
                (2.0f * p0.x - 5.0f * p1.x + 4 * p2.x - p3.x) * t +
                (-p0.x + 3.0f * p1.x - 3.0f * p2.x + p3.x) * t2 * 1.5f;

            ret.y = 0.5f * (-p0.y + p2.y) +
                (2.0f * p0.y - 5.0f * p1.y + 4 * p2.y - p3.y) * t +
                (-p0.y + 3.0f * p1.y - 3.0f * p2.y + p3.y) * t2 * 1.5f;

            ret.z = 0.5f * (-p0.z + p2.z) +
                (2.0f * p0.z - 5.0f * p1.z + 4 * p2.z - p3.z) * t +
                (-p0.z + 3.0f * p1.z - 3.0f * p2.z + p3.z) * t2 * 1.5f;

            return ret;
        }
    }
}