using UnityEngine;
using System.Collections;
using Kyub.Extensions;

namespace Kyub.UI
{
    public class RotationTween : TimeTween
    {
        #region Helper Classes/Enums

        [System.Flags]
        public enum RotationTypeOptionEnum { RotateBy = 1, RotateToX = 4, RotateToY = 8, RotateToZ = 16, RotateToXY = 12, RotateToYZ = 24, RotateToXZ = 20, RotateToXYZ = 28 }
        public enum RotationLerpModeEnum { Quaternion, EulerAngles }

        #endregion

        #region Private Variables

        [SerializeField]
        Vector3 m_rotateVector = Vector2.zero;
        [SerializeField]
        Vector3 m_rotateBackVector = Vector2.zero;
        [SerializeField]
        RotationTypeOptionEnum m_rotationTypeOption = RotationTypeOptionEnum.RotateBy;
        [SerializeField]
        bool m_isLocalRotation = false;
        [SerializeField]
        bool m_useRotateBackVector = false;
        [SerializeField]
        bool m_useVectorsToClampInitialValues = false;
        [SerializeField]
        RotationLerpModeEnum m_lerpMode = RotationLerpModeEnum.Quaternion;

        #endregion

        #region Protected Properties

        protected Quaternion _initialRotation = Quaternion.identity;
        protected Quaternion _finalRotation = Quaternion.identity;

        #endregion

        #region Public Properties

        public Vector3 RotateVector { get { return m_rotateVector; } set { m_rotateVector = value; } }
        public Vector3 RotateBackVector { get { return m_rotateBackVector; } set { m_rotateBackVector = value; } }
        public RotationTypeOptionEnum RotationTypeOption { get { return m_rotationTypeOption; } set { m_rotationTypeOption = value; } }
        public bool IsLocalRotation { get { return m_isLocalRotation; } set { m_isLocalRotation = value; } }
        public bool UseRotateBackVector { get { return m_useRotateBackVector; } set { m_useRotateBackVector = value; } }
        public bool UseVectorsToClampInitialValues { get { return m_useVectorsToClampInitialValues; } set { m_useVectorsToClampInitialValues = value; } }
        public RotationLerpModeEnum LerpMode { get { return m_lerpMode; } set { m_lerpMode = value; } }

        #endregion

        #region Overridden Methods

        protected override void OnPingStart()
        {
            InitRotationToMoveBy(true);
        }

        protected override void OnPongStart()
        {
            InitRotationToMoveBy(false);
        }

        protected override void OnPingUpdate()
        {
            SetAmountToRotate(true, GetTimeScale());
        }

        protected override void OnPongUpdate()
        {
            SetAmountToRotate(false, 1 - GetTimeScale());
        }

        /*protected override void OnPingFinish()
        {
            SetAmountToRotate(true, 1f);
        }

        protected override void OnPongFinish()
        {
            SetAmountToRotate(false, 1f);
        }*/

        #endregion

        #region Other Methods

        private void CheckIfCanClampInitialValues(bool p_isPing)
        {
            if (Target != null)
            {
                if (UseVectorsToClampInitialValues && RotationTypeOption != RotationTypeOptionEnum.RotateBy)
                {
                    if (p_isPing)
                    {
                        if (IsLocalRotation)
                        {
                            Vector3 v_vectorLocal = new Vector3(Target.localEulerAngles.x, Target.localEulerAngles.y, Target.localEulerAngles.z);
                            v_vectorLocal.x = EnumExtensions.ContainsFlag(RotationTypeOption, RotationTypeOptionEnum.RotateToX) ? RotateBackVector.x : v_vectorLocal.x;
                            v_vectorLocal.y = EnumExtensions.ContainsFlag(RotationTypeOption, RotationTypeOptionEnum.RotateToY) ? RotateBackVector.y : v_vectorLocal.y;
                            v_vectorLocal.z = EnumExtensions.ContainsFlag(RotationTypeOption, RotationTypeOptionEnum.RotateToZ) ? RotateBackVector.z : v_vectorLocal.z;
                            Target.localEulerAngles = v_vectorLocal;
                        }
                        else
                        {
                            Vector3 v_vectorGlobal = new Vector3(Target.eulerAngles.x, Target.eulerAngles.y, Target.eulerAngles.z);
                            v_vectorGlobal.x = EnumExtensions.ContainsFlag(RotationTypeOption, RotationTypeOptionEnum.RotateToX) ? RotateBackVector.x : v_vectorGlobal.x;
                            v_vectorGlobal.y = EnumExtensions.ContainsFlag(RotationTypeOption, RotationTypeOptionEnum.RotateToY) ? RotateBackVector.y : v_vectorGlobal.y;
                            v_vectorGlobal.z = EnumExtensions.ContainsFlag(RotationTypeOption, RotationTypeOptionEnum.RotateToZ) ? RotateBackVector.z : v_vectorGlobal.z;
                            Target.eulerAngles = v_vectorGlobal;
                        }
                    }
                    else if (UseRotateBackVector)
                    {
                        if (IsLocalRotation)
                        {
                            Target.localEulerAngles = RotateVector;
                        }
                        else
                        {
                            Target.eulerAngles = RotateVector;
                        }
                    }
                }
            }
        }

        public void InitRotationToMoveBy(bool p_isPing)
        {
            if (Target != null)
            {
                CheckIfCanClampInitialValues(p_isPing);
                
                Vector3 v_rotateVector = !p_isPing && UseRotateBackVector ? RotateBackVector : RotateVector;
                if (RotationTypeOption == RotationTypeOptionEnum.RotateBy)
                {
                    _initialRotation = m_isLocalRotation ? Target.localRotation : Target.rotation;
                    _finalRotation = Quaternion.Euler(_initialRotation.eulerAngles + v_rotateVector);
                }
                else if (p_isPing || (!p_isPing && UseRotateBackVector))
                {
                    var v_initialEuler = m_isLocalRotation ? Target.localEulerAngles : Target.eulerAngles;
                    var v_finalEuler = v_rotateVector;

                    if (RotationTypeOption == RotationTypeOptionEnum.RotateToX)
                        v_finalEuler = new Vector3(v_finalEuler.x, v_initialEuler.y, v_initialEuler.z);
                    else if (RotationTypeOption == RotationTypeOptionEnum.RotateToY)
                        v_finalEuler = new Vector3(v_initialEuler.x, v_finalEuler.y, v_initialEuler.z);
                    else if (RotationTypeOption == RotationTypeOptionEnum.RotateToZ)
                        v_finalEuler = new Vector3(v_initialEuler.x, v_initialEuler.y, v_finalEuler.z);
                    else if (RotationTypeOption == RotationTypeOptionEnum.RotateToXY)
                        v_finalEuler = new Vector3(v_finalEuler.x, v_finalEuler.y, v_initialEuler.z);
                    else if (RotationTypeOption == RotationTypeOptionEnum.RotateToXZ)
                        v_finalEuler = new Vector3(v_finalEuler.x, v_initialEuler.y, v_finalEuler.z);
                    else if (RotationTypeOption == RotationTypeOptionEnum.RotateToYZ)
                        v_finalEuler = new Vector3(v_initialEuler.x, v_finalEuler.y, v_finalEuler.z);

                    _initialRotation = Quaternion.Euler(v_initialEuler);
                    _finalRotation = Quaternion.Euler(v_finalEuler);
                }
            }
        }

        #endregion

        #region Gets and Sets

        protected virtual void SetAmountToRotate(bool p_isPing, float p_timeScale)
        {
            if (Target != null)
            {
                /*float v_timeScale = p_timeScale;
                Vector3 v_timeScaleVector = new Vector3(DistanceToRotateBy.x * v_timeScale, DistanceToRotateBy.y * v_timeScale, DistanceToRotateBy.z * v_timeScale);
                Vector3 v_vectorToAdd = new Vector3(v_timeScaleVector.x - CurrentDistanceRotatedBy.x, v_timeScaleVector.y - CurrentDistanceRotatedBy.y, v_timeScaleVector.z - CurrentDistanceRotatedBy.z);
                CurrentDistanceRotatedBy = v_timeScaleVector;
                if (!p_isPing && !UseRotateBackVector)
                    v_vectorToAdd = new Vector3(-v_vectorToAdd.x, -v_vectorToAdd.y, -v_vectorToAdd.z);
                if (m_isLocalRotation)
                    Target.localEulerAngles = new Vector3(Target.localEulerAngles.x + v_vectorToAdd.x, Target.localEulerAngles.y + v_vectorToAdd.y, Target.localEulerAngles.z + v_vectorToAdd.z);
                else
                    Target.eulerAngles = new Vector3(Target.eulerAngles.x + v_vectorToAdd.x, Target.eulerAngles.y + v_vectorToAdd.y, Target.eulerAngles.z + v_vectorToAdd.z);*/

                //if (!p_isPing && !UseRotateBackVector)
                //    v_vectorToAdd = new Vector3(-v_vectorToAdd.x, -v_vectorToAdd.y, -v_vectorToAdd.z);
                if (m_lerpMode == RotationLerpModeEnum.Quaternion)
                {
                    if (m_isLocalRotation)
                        Target.localRotation = Quaternion.Lerp(_initialRotation, _finalRotation, p_timeScale);
                    else
                        Target.rotation = Quaternion.Lerp(_initialRotation, _finalRotation, p_timeScale);
                }
                else
                {
                    if (m_isLocalRotation)
                        Target.localEulerAngles = Vector3.Lerp(_initialRotation.eulerAngles, _finalRotation.eulerAngles, p_timeScale);
                    else
                        Target.eulerAngles = Vector3.Lerp(_initialRotation.eulerAngles, _finalRotation.eulerAngles, p_timeScale);
                }
            }
        }

        #endregion
    }
}
