// ----------------------------------------------------------------------------
// <copyright file="PhotonTransformViewRotationModel.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2016 Exit Games GmbH
// </copyright>
// <summary>
//   Model class to synchronize rotations via PUN PhotonView.
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------

using System;

[Serializable]
public class PhotonTransformViewRotationModel
{
    public enum InterpolateOptions
    {
        Disabled,
        RotateTowards,
        Lerp
    }

    public float InterpolateLerpSpeed = 5;

    public InterpolateOptions InterpolateOption = InterpolateOptions.RotateTowards;
    public float InterpolateRotateTowardsSpeed = 180;

    public bool SynchronizeEnabled;
}