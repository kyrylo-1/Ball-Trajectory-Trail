using UnityEngine;
using System.Collections;
using System;

// Ball Trajectory & Trail
// Kyrylo Avramenko 
// https://github.com/kir-avramenko

namespace Shorka.BallTrajectory
{
    public enum PullState
    {
        Idle,
        UserPulling,
        ObjFlying
    }

    /// <summary>
    /// Resposible for pulling mechanics and
    /// </summary>
    public class PullCtrl : MonoBehaviour
    {
        #region fields
        //[SerializeField] private Transform throwTarget;
        [SerializeField] private ThrownObject throwObj;

        [SerializeField] private Transform dotHelper;
        [SerializeField] private Transform pullingStartPoint;

        [Space(5)]
        [Tooltip("this linerenderer will draw the projected trajectory of the thrown object")]
        [SerializeField]
        private LineRenderer trajectoryLineRen;

        [SerializeField]
        private TrailMaker trail;

        [Space(5)]
        [SerializeField]
        private float throwSpeed = 10F;

        [Tooltip("Max Distance between 'PullingStartPoint' and pulling touch point")]
        [SerializeField]
        private float maxDistance = 1.5F;

        [SerializeField]
        private float coofDotHelper = 1.5F;

        [Tooltip("Related to length of trajectory line")]
        [SerializeField]
        private int qtyOfsegments = 13;

        [Tooltip("Step of changing trajectory dots offset in runtime")]
        [SerializeField]
        private float stepMatOffset = 0.01F;

        [Tooltip("Z position of trajectory dots")]
        [SerializeField]
        private float dotPosZ = 0F;

        private PullState pullState;
        private Camera camMain;
        //private Collider2D collThrowTarget;
        private Rigidbody2D rgThrowTarget;

        private Vector3 posPullingStart;
        private Vector3 initPos;

        private TrajectoryCtrl trajCtrl;
        #endregion

        public Vector3 PosDotHelper { get { return dotHelper.position; } }
        public Vector3 PosThrowTarget { get { return throwObj.transform.position; } }

        public int QtyOfsegments { get { return qtyOfsegments; } }
        public float DotPosZ { get { return dotPosZ; } }
        public Vector3 PosPullingStart { get { return posPullingStart; } }
        public float StepMatOffset { get { return stepMatOffset; } }

        void Awake()
        {
            trail.emit = false;
            trajCtrl = new TrajectoryCtrl(this, trajectoryLineRen);
        }

        void Start()
        {
            camMain = Camera.main;
            pullState = PullState.Idle;
            posPullingStart = pullingStartPoint.position;
            initPos = PosThrowTarget;
        }

        void Update()
        {
            SwitchStates();

            if (Input.GetKeyDown(KeyCode.R))
            {
                Debug.Log("Restart object states");
                Restart(initPos);
            }
        }

        private void SwitchStates()
        {
            switch (pullState)
            {
                case PullState.Idle:

                    if (Input.GetMouseButtonDown(0))
                    {
                        //get the point on screen user has tapped
                        Vector3 location = camMain.ScreenToWorldPoint(Input.mousePosition);
                        //if user has tapped onto the ball
                        if (throwObj.Collider == Physics2D.OverlapPoint(location))
                            pullState = PullState.UserPulling;
                    }
                    break;
                case PullState.UserPulling:

                    dotHelper.gameObject.SetActive(true);

                    if (Input.GetMouseButton(0))
                    {
                        //get touch position
                        Vector3 posMouse = camMain.ScreenToWorldPoint(Input.mousePosition);
                        posMouse.z = 0;
                        //we will let the user pull the ball up to a maximum distance
                        if (Vector3.Distance(posMouse, posPullingStart) > maxDistance)
                        {
                            Vector3 maxPosition = (posMouse - posPullingStart).normalized * maxDistance + posPullingStart;
                            maxPosition.z = dotHelper.position.z;
                            dotHelper.position = maxPosition;
                        }
                        else
                        {
                            posMouse.z = dotHelper.position.z;
                            dotHelper.position = posMouse;
                        }

                        float distance = Vector3.Distance(posPullingStart, dotHelper.position);
                        trajCtrl.DisplayTrajectory(distance);
                    }
                    else//user has removed the tap 
                    {
                        float distance = Vector3.Distance(posPullingStart, dotHelper.position);
                        trajectoryLineRen.enabled = false;
                        ThrowObj(distance);
                    }
                    break;

                default:
                    break;
            }
        }


        //private Vector2 velocityToRg = Vector2.zero;
        private void ThrowObj(float distance)
        {
            Debug.Log("ThrowObj");

            pullState = PullState.Idle;
            Vector3 velocity = posPullingStart - dotHelper.position;
            //velocityToRg = CalcVelocity(velocity, distance);

            throwObj.ThrowObj(CalcVelocity(velocity, distance));
            //rgThrowTarget.velocity = velocityToRg;
            //rgThrowTarget.isKinematic = false;

            trail.enabled = true;
            trail.emit = true;
            dotHelper.gameObject.SetActive(false);
        }

        /// <summary>
        /// Restart thrown object states, clear trail
        /// </summary>
        /// <param name="posThrownObj"></param>
        public void Restart(Vector3 posThrownObj)
        {
            trail.emit = false;
            trail.Clear();

            StartCoroutine(ClearTrail());

            trajectoryLineRen.enabled = false;
            dotHelper.gameObject.SetActive(false);
            pullState = PullState.Idle;

            throwObj.Reset(posThrownObj);
        }

        private readonly WaitForSeconds wtTimeBeforeClear = new WaitForSeconds(0.3F);
        IEnumerator ClearTrail()
        {
            yield return wtTimeBeforeClear;
            trail.Clear();
            trail.enabled = false;
        }

        Vector3 velocity = Vector3.zero;
        public Vector3 CalcVelocity(Vector3 diff, float distance)
        {
            velocity.x = diff.x * throwSpeed * distance * coofDotHelper;
            velocity.y = diff.y * throwSpeed * distance * coofDotHelper;

            return velocity;
        }
    }
}
