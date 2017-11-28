using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Ball Trajectory & Trail
// Kyrylo Avramenko 
// https://github.com/kir-avramenko

namespace Shorka.BallTrajectory
{
    /// <summary>
    /// In Charge of showing trajectory
    /// </summary>
    public class TrajectoryCtrl
    {
        private readonly PullCtrl pullCtrl;
        private readonly LineRenderer trajecLineRen;
        private Vector3[] segments;

        public TrajectoryCtrl(PullCtrl pullCtrl, LineRenderer trajecLineRen)
        {
            this.pullCtrl = pullCtrl;
            this.trajecLineRen = trajecLineRen;

            segments = new Vector3[pullCtrl.QtyOfsegments];
        }

        private Vector2 segVelocity = Vector2.zero;
        private float shiftX = 0F;
        private const string OFFSET = "_Offset";

        /// <summary>
        /// Display projected trajectory based on the distance
        /// </summary>
        /// <param name="distance"></param>
        public void DisplayTrajectory(float distance)
        {
            trajecLineRen.enabled = true;
            Vector3 diff = pullCtrl.PosPullingStart - pullCtrl.PosDotHelper;

            // The 1st point is wherever the thrown object is
            Vector3 seg0 = pullCtrl.PosThrowTarget;
            seg0.z = pullCtrl.DotPosZ;
            segments[0] = seg0;
            Vector2 seg0Vec2 = seg0;

            // The initial velocity
            segVelocity = pullCtrl.CalcVelocity(diff, distance);

            for (int i = 1; i < pullCtrl.QtyOfsegments; i++)
            {
                float time2 = i * Time.fixedDeltaTime * 5;
                Vector3 iPos = seg0Vec2 + segVelocity * time2 + 0.5f * Physics2D.gravity * (time2 * time2);

                iPos.z = pullCtrl.DotPosZ;
                segments[i] = iPos;
            }

            trajecLineRen.positionCount = pullCtrl.QtyOfsegments;

            //obsolete version of trajectoryLineRen.positionCount = qtyOfsegments;
            //trajectoryLineRen.SetVertexCount(qtyOfsegments);

            for (int i = 0; i < pullCtrl.QtyOfsegments; i++)
            {
                trajecLineRen.SetPosition(i, segments[i]);
            }

            shiftX += pullCtrl.StepMatOffset;
            trajecLineRen.material.SetFloat(OFFSET, shiftX);
        }
    }
}