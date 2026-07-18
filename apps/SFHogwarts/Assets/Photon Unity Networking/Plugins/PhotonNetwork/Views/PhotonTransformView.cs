// ----------------------------------------------------------------------------
// <copyright file="PhotonTransformView.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2016 Exit Games GmbH
// </copyright>
// <summary>
//   Component to synchronize Transforms via PUN PhotonView.
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------

using UnityEngine;

/// <summary>
///     This class helps you to synchronize position, rotation and scale
///     of a GameObject. It also gives you many different options to make
///     the synchronized values appear smooth, even when the data is only
///     send a couple of times per second.
///     Simply add the component to your GameObject and make sure that
///     the PhotonTransformView is added to the list of observed components
/// </summary>
[RequireComponent(typeof(PhotonView))]
[AddComponentMenu("Photon Networking/Photon Transform View")]
public class PhotonTransformView : MonoBehaviour, IPunObservable
{
    /// <summary>
    ///     Flag to skip initial data when Object is instantiated and rely on the first deserialized data instead.
    /// </summary>
    private bool m_firstTake;

    private PhotonView m_PhotonView;

    private PhotonTransformViewPositionControl m_PositionControl;
    //Since this component is very complex, we seperated it into multiple objects.
    //The PositionModel, RotationModel and ScaleMode store the data you are able to
    //configure in the inspector while the control objects below are actually moving
    //the object and calculating all the inter- and extrapolation

    [SerializeField] public PhotonTransformViewPositionModel m_PositionModel = new();

    private bool m_ReceivedNetworkUpdate;
    private PhotonTransformViewRotationControl m_RotationControl;

    [SerializeField] public PhotonTransformViewRotationModel m_RotationModel = new();

    private PhotonTransformViewScaleControl m_ScaleControl;

    [SerializeField] public PhotonTransformViewScaleModel m_ScaleModel = new();

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        m_PositionControl.OnPhotonSerializeView(transform.localPosition, stream, info);
        m_RotationControl.OnPhotonSerializeView(transform.localRotation, stream, info);
        m_ScaleControl.OnPhotonSerializeView(transform.localScale, stream, info);

        if (m_PhotonView.isMine == false && m_PositionModel.DrawErrorGizmo) DoDrawEstimatedPositionError();

        if (stream.isReading)
        {
            m_ReceivedNetworkUpdate = true;

            // force latest data to avoid initial drifts when player is instantiated.
            if (m_firstTake)
            {
                m_firstTake = false;

                if (m_PositionModel.SynchronizeEnabled)
                    transform.localPosition = m_PositionControl.GetNetworkPosition();

                if (m_RotationModel.SynchronizeEnabled)
                    transform.localRotation = m_RotationControl.GetNetworkRotation();

                if (m_ScaleModel.SynchronizeEnabled) transform.localScale = m_ScaleControl.GetNetworkScale();
            }
        }
    }

    private void Awake()
    {
        m_PhotonView = GetComponent<PhotonView>();

        m_PositionControl = new PhotonTransformViewPositionControl(m_PositionModel);
        m_RotationControl = new PhotonTransformViewRotationControl(m_RotationModel);
        m_ScaleControl = new PhotonTransformViewScaleControl(m_ScaleModel);
    }

    private void OnEnable()
    {
        m_firstTake = true;
    }

    private void Update()
    {
        if (m_PhotonView == null || m_PhotonView.isMine || PhotonNetwork.connected == false) return;

        UpdatePosition();
        UpdateRotation();
        UpdateScale();
    }

    private void UpdatePosition()
    {
        if (m_PositionModel.SynchronizeEnabled == false || m_ReceivedNetworkUpdate == false) return;

        transform.localPosition = m_PositionControl.UpdatePosition(transform.localPosition);
    }

    private void UpdateRotation()
    {
        if (m_RotationModel.SynchronizeEnabled == false || m_ReceivedNetworkUpdate == false) return;

        transform.localRotation = m_RotationControl.GetRotation(transform.localRotation);
    }

    private void UpdateScale()
    {
        if (m_ScaleModel.SynchronizeEnabled == false || m_ReceivedNetworkUpdate == false) return;

        transform.localScale = m_ScaleControl.GetScale(transform.localScale);
    }

    /// <summary>
    ///     These values are synchronized to the remote objects if the interpolation mode
    ///     or the extrapolation mode SynchronizeValues is used. Your movement script should pass on
    ///     the current speed (in units/second) and turning speed (in angles/second) so the remote
    ///     object can use them to predict the objects movement.
    /// </summary>
    /// <param name="speed">The current movement vector of the object in units/second.</param>
    /// <param name="turnSpeed">The current turn speed of the object in angles/second.</param>
    public void SetSynchronizedValues(Vector3 speed, float turnSpeed)
    {
        m_PositionControl.SetSynchronizedValues(speed, turnSpeed);
    }

    //void OnDrawGizmos()
    //{
    //    if( Application.isPlaying == false || m_PhotonView == null || m_PhotonView.isMine == true || PhotonNetwork.connected == false )
    //    {
    //        return;
    //    }

    //    DoDrawNetworkPositionGizmo();
    //    DoDrawExtrapolatedPositionGizmo();
    //}

    private void DoDrawEstimatedPositionError()
    {
        var targetPosition = m_PositionControl.GetNetworkPosition();

        // we are synchronizing the localPosition, so we need to add the parent position for a proper positioning.
        if (transform.parent != null) targetPosition = transform.parent.position + targetPosition;

        Debug.DrawLine(targetPosition, transform.position, Color.red, 2f);
        Debug.DrawLine(transform.position, transform.position + Vector3.up, Color.green, 2f);
        Debug.DrawLine(targetPosition, targetPosition + Vector3.up, Color.red, 2f);
    }

    //void DoDrawNetworkPositionGizmo()
    //{
    //    if( m_PositionModel.DrawNetworkGizmo == false || m_PositionControl == null )
    //    {
    //        return;
    //    }

    //    ExitGames.Client.GUI.GizmoTypeDrawer.Draw( m_PositionControl.GetNetworkPosition(),
    //                                               m_PositionModel.NetworkGizmoType,
    //                                               m_PositionModel.NetworkGizmoColor,
    //                                               m_PositionModel.NetworkGizmoSize );
    //}

    //void DoDrawExtrapolatedPositionGizmo()
    //{
    //    if( m_PositionModel.DrawExtrapolatedGizmo == false ||
    //        m_PositionModel.ExtrapolateOption == PhotonTransformViewPositionModel.ExtrapolateOptions.Disabled ||
    //        m_PositionControl == null )
    //    {
    //        return;
    //    }

    //    ExitGames.Client.GUI.GizmoTypeDrawer.Draw( m_PositionControl.GetNetworkPosition() + m_PositionControl.GetExtrapolatedPositionOffset(),
    //                                               m_PositionModel.ExtrapolatedGizmoType,
    //                                               m_PositionModel.ExtrapolatedGizmoColor,
    //                                               m_PositionModel.ExtrapolatedGizmoSize );
    //}
}