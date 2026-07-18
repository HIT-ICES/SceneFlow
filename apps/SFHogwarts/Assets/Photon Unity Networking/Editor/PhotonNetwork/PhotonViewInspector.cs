// ----------------------------------------------------------------------------
// <copyright file="PhotonViewInspector.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2011 Exit Games GmbH
// </copyright>
// <summary>
//   Custom inspector for the PhotonView component.
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------

#if UNITY_5 && !UNITY_5_0 && !UNITY_5_1 && !UNITY_5_2 || UNITY_5_4_OR_NEWER
#define UNITY_MIN_5_3
#endif

#pragma warning disable 618

using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PhotonView))]
public class PhotonViewInspector : Editor
{
    private PhotonView m_Target;

    public override void OnInspectorGUI()
    {
        m_Target = (PhotonView)target;
        var isProjectPrefab = PhotonEditorUtils.IsPrefab(m_Target.gameObject);

        if (m_Target.ObservedComponents == null) m_Target.ObservedComponents = new List<Component>();

        if (m_Target.ObservedComponents.Count == 0) m_Target.ObservedComponents.Add(null);

        EditorGUILayout.BeginHorizontal();
        // Owner
        if (isProjectPrefab)
        {
            EditorGUILayout.LabelField("Owner:", "Set at runtime");
        }
        else if (!m_Target.isOwnerActive)
        {
            EditorGUILayout.LabelField("Owner", "Scene");
        }
        else
        {
            var owner = m_Target.owner;
            var ownerInfo = owner != null ? owner.NickName : "<no PhotonPlayer found>";

            if (string.IsNullOrEmpty(ownerInfo)) ownerInfo = "<no playername set>";

            EditorGUILayout.LabelField("Owner", "[" + m_Target.ownerId + "] " + ownerInfo);
        }

        // ownership requests
        EditorGUI.BeginDisabledGroup(Application.isPlaying);
        var own = (OwnershipOption)EditorGUILayout.EnumPopup(m_Target.ownershipTransfer, GUILayout.Width(100));
        if (own != m_Target.ownershipTransfer)
        {
            // jf: fixed 5 and up prefab not accepting changes if you quit Unity straight after change.
            // not touching the define nor the rest of the code to avoid bringing more problem than solving.
            EditorUtility.SetDirty(m_Target);

            Undo.RecordObject(m_Target, "Change PhotonView Ownership Transfer");
            m_Target.ownershipTransfer = own;
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.EndHorizontal();


        // View ID
        if (isProjectPrefab)
        {
            EditorGUILayout.LabelField("View ID", "Set at runtime");
        }
        else if (EditorApplication.isPlaying)
        {
            EditorGUILayout.LabelField("View ID", m_Target.viewID.ToString());
        }
        else
        {
            var idValue = EditorGUILayout.IntField("View ID [1.." + (PhotonNetwork.MAX_VIEW_IDS - 1) + "]",
                m_Target.viewID);
            if (m_Target.viewID != idValue)
            {
                Undo.RecordObject(m_Target, "Change PhotonView viewID");
                m_Target.viewID = idValue;
            }
        }

        // Locally Controlled
        if (EditorApplication.isPlaying)
        {
            var masterClientHint = PhotonNetwork.isMasterClient ? "(master)" : "";
            EditorGUILayout.Toggle("Controlled locally: " + masterClientHint, m_Target.isMine);
        }

        // ViewSynchronization (reliability)
        if (m_Target.synchronization == ViewSynchronization.Off) GUI.color = Color.grey;

        EditorGUILayout.PropertyField(serializedObject.FindProperty("synchronization"),
            new GUIContent("Observe option:"));

        if (m_Target.synchronization != ViewSynchronization.Off &&
            m_Target.ObservedComponents.FindAll(item => item != null).Count == 0)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Warning", EditorStyles.boldLabel);
            GUILayout.Label("Setting the synchronization option only makes sense if you observe something.");
            GUILayout.EndVertical();
        }

        DrawSpecificTypeSerializationOptions();

        GUI.color = Color.white;
        DrawObservedComponentsList();

        // Cleanup: save and fix look
        if (GUI.changed)
        {
#if !UNITY_MIN_5_3
            EditorUtility.SetDirty(this.m_Target);
#endif
            PhotonViewHandler.HierarchyChange(); // TODO: check if needed
        }

        GUI.color = Color.white;
#if !UNITY_MIN_5_3
        EditorGUIUtility.LookLikeControls();
#endif
    }

    private void DrawSpecificTypeSerializationOptions()
    {
        if (m_Target.ObservedComponents.FindAll(item => item != null && item.GetType() == typeof(Transform)).Count > 0)
            m_Target.onSerializeTransformOption =
                (OnSerializeTransform)EditorGUILayout.EnumPopup("Transform Serialization:",
                    m_Target.onSerializeTransformOption);
        else if (m_Target.ObservedComponents.FindAll(item => item != null && item.GetType() == typeof(Rigidbody))
                     .Count > 0 ||
                 m_Target.ObservedComponents.FindAll(item => item != null && item.GetType() == typeof(Rigidbody2D))
                     .Count > 0)
            m_Target.onSerializeRigidBodyOption =
                (OnSerializeRigidBody)EditorGUILayout.EnumPopup("Rigidbody Serialization:",
                    m_Target.onSerializeRigidBodyOption);
    }


    private int GetObservedComponentsCount()
    {
        var count = 0;

        for (var i = 0; i < m_Target.ObservedComponents.Count; ++i)
            if (m_Target.ObservedComponents[i] != null)
                count++;

        return count;
    }

    private void DrawObservedComponentsList()
    {
        GUILayout.Space(5);
        var listProperty = serializedObject.FindProperty("ObservedComponents");

        if (listProperty == null) return;

        float containerElementHeight = 22;
        var containerHeight = listProperty.arraySize * containerElementHeight;

        var isOpen = PhotonGUI.ContainerHeaderFoldout("Observed Components (" + GetObservedComponentsCount() + ")",
            serializedObject.FindProperty("ObservedComponentsFoldoutOpen").boolValue);
        serializedObject.FindProperty("ObservedComponentsFoldoutOpen").boolValue = isOpen;

        if (isOpen == false) containerHeight = 0;

        //Texture2D statsIcon = AssetDatabase.LoadAssetAtPath( "Assets/Photon Unity Networking/Editor/PhotonNetwork/PhotonViewStats.png", typeof( Texture2D ) ) as Texture2D;

        var containerRect = PhotonGUI.ContainerBody(containerHeight);
        var wasObservedComponentsEmpty = m_Target.ObservedComponents.FindAll(item => item != null).Count == 0;
        if (isOpen)
            for (var i = 0; i < listProperty.arraySize; ++i)
            {
                var elementRect = new Rect(containerRect.xMin, containerRect.yMin + containerElementHeight * i,
                    containerRect.width, containerElementHeight);
                {
                    var texturePosition = new Rect(elementRect.xMin + 6, elementRect.yMin + elementRect.height / 2f - 1,
                        9, 5);
                    ReorderableListResources.DrawTexture(texturePosition, ReorderableListResources.texGrabHandle);

                    var propertyPosition = new Rect(elementRect.xMin + 20, elementRect.yMin + 3, elementRect.width - 45,
                        16);
                    EditorGUI.PropertyField(propertyPosition, listProperty.GetArrayElementAtIndex(i), new GUIContent());

                    //Debug.Log( listProperty.GetArrayElementAtIndex( i ).objectReferenceValue.GetType() );
                    //Rect statsPosition = new Rect( propertyPosition.xMax + 7, propertyPosition.yMin, statsIcon.width, statsIcon.height );
                    //ReorderableListResources.DrawTexture( statsPosition, statsIcon );
                    var removeButtonRect = new Rect(elementRect.xMax - PhotonGUI.DefaultRemoveButtonStyle.fixedWidth,
                        elementRect.yMin + 2,
                        PhotonGUI.DefaultRemoveButtonStyle.fixedWidth,
                        PhotonGUI.DefaultRemoveButtonStyle.fixedHeight);

                    GUI.enabled = listProperty.arraySize > 1;
                    if (GUI.Button(removeButtonRect, new GUIContent(ReorderableListResources.texRemoveButton),
                            PhotonGUI.DefaultRemoveButtonStyle)) listProperty.DeleteArrayElementAtIndex(i);
                    GUI.enabled = true;

                    if (i < listProperty.arraySize - 1)
                    {
                        texturePosition = new Rect(elementRect.xMin + 2, elementRect.yMax, elementRect.width - 4, 1);
                        PhotonGUI.DrawSplitter(texturePosition);
                    }
                }
            }

        if (PhotonGUI.AddButton()) listProperty.InsertArrayElementAtIndex(Mathf.Max(0, listProperty.arraySize - 1));

        serializedObject.ApplyModifiedProperties();

        var isObservedComponentsEmpty = m_Target.ObservedComponents.FindAll(item => item != null).Count == 0;

        if (wasObservedComponentsEmpty && isObservedComponentsEmpty == false &&
            m_Target.synchronization == ViewSynchronization.Off)
        {
            Undo.RecordObject(m_Target, "Change PhotonView");
            m_Target.synchronization = ViewSynchronization.UnreliableOnChange;
#if !UNITY_MIN_5_3
            EditorUtility.SetDirty(this.m_Target);
#endif
            serializedObject.Update();
        }

        if (wasObservedComponentsEmpty == false && isObservedComponentsEmpty)
        {
            Undo.RecordObject(m_Target, "Change PhotonView");
            m_Target.synchronization = ViewSynchronization.Off;
#if !UNITY_MIN_5_3
            EditorUtility.SetDirty(this.m_Target);
#endif
            serializedObject.Update();
        }
    }

    private static GameObject GetPrefabParent(GameObject mp)
    {
#if UNITY_2_6_1 || UNITY_2_6 || UNITY_3_0 || UNITY_3_0_0 || UNITY_3_1 || UNITY_3_2 || UNITY_3_3 || UNITY_3_4
        // Unity 3.4 and older use EditorUtility
        return (EditorUtility.GetPrefabParent(mp) as GameObject);
#else
        // Unity 3.5 uses PrefabUtility
        return PrefabUtility.GetPrefabParent(mp) as GameObject;
#endif
    }
}