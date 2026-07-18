// ----------------------------------------------------------------------------
// <copyright file="PhotonTransformViewScaleModel.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2016 Exit Games GmbH
// </copyright>
// <summary>
//   Model to synchronize scale via PUN PhotonView.
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------

using System;

[Serializable]
public class PhotonTransformViewScaleModel
{
    public enum InterpolateOptions
    {
        Disabled,
        MoveTowards,
        Lerp
    }

    public float InterpolateLerpSpeed;
    public float InterpolateMoveTowardsSpeed = 1f;

    public InterpolateOptions InterpolateOption = InterpolateOptions.Disabled;

    public bool SynchronizeEnabled;
}