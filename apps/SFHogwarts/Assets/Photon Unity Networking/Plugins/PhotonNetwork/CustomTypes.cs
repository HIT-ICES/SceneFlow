// ----------------------------------------------------------------------------
// <copyright file="CustomTypes.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2011 Exit Games GmbH
// </copyright>
// <summary>
//
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------

#pragma warning disable 1587
/// \file
/// <summary>Sets up support for Unity-specific types. Can be a blueprint how to register your own Custom Types for sending.</summary>
#pragma warning restore 1587


using ExitGames.Client.Photon;
using UnityEngine;

/// <summary>
///     Internally used class, containing de/serialization methods for various Unity-specific classes.
///     Adding those to the Photon serialization protocol allows you to send them in events, etc.
/// </summary>
internal static class CustomTypes
{
    /// <summary>Register</summary>
    internal static void Register()
    {
        PhotonPeer.RegisterType(typeof(Vector2), (byte)'W', SerializeVector2, DeserializeVector2);
        PhotonPeer.RegisterType(typeof(Vector3), (byte)'V', SerializeVector3, DeserializeVector3);
        PhotonPeer.RegisterType(typeof(Quaternion), (byte)'Q', SerializeQuaternion, DeserializeQuaternion);
        PhotonPeer.RegisterType(typeof(PhotonPlayer), (byte)'P', SerializePhotonPlayer, DeserializePhotonPlayer);
    }


    #region Custom De/Serializer Methods

    public static readonly byte[] memVector3 = new byte[3 * 4];

    private static short SerializeVector3(StreamBuffer outStream, object customobject)
    {
        var vo = (Vector3)customobject;

        var index = 0;
        lock (memVector3)
        {
            var bytes = memVector3;
            Protocol.Serialize(vo.x, bytes, ref index);
            Protocol.Serialize(vo.y, bytes, ref index);
            Protocol.Serialize(vo.z, bytes, ref index);
            outStream.Write(bytes, 0, 3 * 4);
        }

        return 3 * 4;
    }

    private static object DeserializeVector3(StreamBuffer inStream, short length)
    {
        var vo = new Vector3();
        lock (memVector3)
        {
            inStream.Read(memVector3, 0, 3 * 4);
            var index = 0;
            Protocol.Deserialize(out vo.x, memVector3, ref index);
            Protocol.Deserialize(out vo.y, memVector3, ref index);
            Protocol.Deserialize(out vo.z, memVector3, ref index);
        }

        return vo;
    }


    public static readonly byte[] memVector2 = new byte[2 * 4];

    private static short SerializeVector2(StreamBuffer outStream, object customobject)
    {
        var vo = (Vector2)customobject;
        lock (memVector2)
        {
            var bytes = memVector2;
            var index = 0;
            Protocol.Serialize(vo.x, bytes, ref index);
            Protocol.Serialize(vo.y, bytes, ref index);
            outStream.Write(bytes, 0, 2 * 4);
        }

        return 2 * 4;
    }

    private static object DeserializeVector2(StreamBuffer inStream, short length)
    {
        var vo = new Vector2();
        lock (memVector2)
        {
            inStream.Read(memVector2, 0, 2 * 4);
            var index = 0;
            Protocol.Deserialize(out vo.x, memVector2, ref index);
            Protocol.Deserialize(out vo.y, memVector2, ref index);
        }

        return vo;
    }


    public static readonly byte[] memQuarternion = new byte[4 * 4];

    private static short SerializeQuaternion(StreamBuffer outStream, object customobject)
    {
        var o = (Quaternion)customobject;

        lock (memQuarternion)
        {
            var bytes = memQuarternion;
            var index = 0;
            Protocol.Serialize(o.w, bytes, ref index);
            Protocol.Serialize(o.x, bytes, ref index);
            Protocol.Serialize(o.y, bytes, ref index);
            Protocol.Serialize(o.z, bytes, ref index);
            outStream.Write(bytes, 0, 4 * 4);
        }

        return 4 * 4;
    }

    private static object DeserializeQuaternion(StreamBuffer inStream, short length)
    {
        var o = new Quaternion();

        lock (memQuarternion)
        {
            inStream.Read(memQuarternion, 0, 4 * 4);
            var index = 0;
            Protocol.Deserialize(out o.w, memQuarternion, ref index);
            Protocol.Deserialize(out o.x, memQuarternion, ref index);
            Protocol.Deserialize(out o.y, memQuarternion, ref index);
            Protocol.Deserialize(out o.z, memQuarternion, ref index);
        }

        return o;
    }

    public static readonly byte[] memPlayer = new byte[4];

    private static short SerializePhotonPlayer(StreamBuffer outStream, object customobject)
    {
        var ID = ((PhotonPlayer)customobject).ID;

        lock (memPlayer)
        {
            var bytes = memPlayer;
            var off = 0;
            Protocol.Serialize(ID, bytes, ref off);
            outStream.Write(bytes, 0, 4);
            return 4;
        }
    }

    private static object DeserializePhotonPlayer(StreamBuffer inStream, short length)
    {
        int ID;
        lock (memPlayer)
        {
            inStream.Read(memPlayer, 0, length);
            var off = 0;
            Protocol.Deserialize(out ID, memPlayer, ref off);
        }

        if (PhotonNetwork.networkingPeer.mActors.ContainsKey(ID))
            return PhotonNetwork.networkingPeer.mActors[ID];
        return null;
    }

    #endregion
}