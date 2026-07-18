#pragma warning disable 1587
/// \file
/// <summary>Part of the [Optional GUI](@ref optionalGui).</summary>
#pragma warning restore 1587


using ExitGames.Client.Photon;
using UnityEngine;

/// <summary>
///     This MonoBehaviour is a basic GUI for the Photon client's network-simulation feature.
///     It can modify lag (fixed delay), jitter (random lag) and packet loss.
/// </summary>
/// \ingroup optionalGui
public class PhotonLagSimulationGui : MonoBehaviour
{
    /// <summary>Shows or hides GUI (does not affect settings).</summary>
    public bool Visible = true;

    /// <summary>Unity GUI Window ID (must be unique or will cause issues).</summary>
    public int WindowId = 101;

    /// <summary>Positioning rect for window.</summary>
    public Rect WindowRect = new(0, 100, 120, 100);

    /// <summary>The peer currently in use (to set the network simulation).</summary>
    public PhotonPeer Peer { get; set; }

    public void Start()
    {
        Peer = PhotonNetwork.networkingPeer;
    }

    public void OnGUI()
    {
        if (!Visible) return;

        if (Peer == null)
            WindowRect = GUILayout.Window(WindowId, WindowRect, NetSimHasNoPeerWindow, "Netw. Sim.");
        else
            WindowRect = GUILayout.Window(WindowId, WindowRect, NetSimWindow, "Netw. Sim.");
    }

    private void NetSimHasNoPeerWindow(int windowId)
    {
        GUILayout.Label("No peer to communicate with. ");
    }

    private void NetSimWindow(int windowId)
    {
        GUILayout.Label(string.Format("Rtt:{0,4} +/-{1,3}", Peer.RoundTripTime, Peer.RoundTripTimeVariance));

        var simEnabled = Peer.IsSimulationEnabled;
        var newSimEnabled = GUILayout.Toggle(simEnabled, "Simulate");
        if (newSimEnabled != simEnabled) Peer.IsSimulationEnabled = newSimEnabled;

        float inOutLag = Peer.NetworkSimulationSettings.IncomingLag;
        GUILayout.Label("Lag " + inOutLag);
        inOutLag = GUILayout.HorizontalSlider(inOutLag, 0, 500);

        Peer.NetworkSimulationSettings.IncomingLag = (int)inOutLag;
        Peer.NetworkSimulationSettings.OutgoingLag = (int)inOutLag;

        float inOutJitter = Peer.NetworkSimulationSettings.IncomingJitter;
        GUILayout.Label("Jit " + inOutJitter);
        inOutJitter = GUILayout.HorizontalSlider(inOutJitter, 0, 100);

        Peer.NetworkSimulationSettings.IncomingJitter = (int)inOutJitter;
        Peer.NetworkSimulationSettings.OutgoingJitter = (int)inOutJitter;

        float loss = Peer.NetworkSimulationSettings.IncomingLossPercentage;
        GUILayout.Label("Loss " + loss);
        loss = GUILayout.HorizontalSlider(loss, 0, 10);

        Peer.NetworkSimulationSettings.IncomingLossPercentage = (int)loss;
        Peer.NetworkSimulationSettings.OutgoingLossPercentage = (int)loss;

        // if anything was clicked, the height of this window is likely changed. reduce it to be layouted again next frame
        if (GUI.changed) WindowRect.height = 100;

        GUI.DragWindow();
    }
}