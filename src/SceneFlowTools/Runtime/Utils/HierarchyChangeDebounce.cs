using System;
using UnityEditor;

namespace SceneFlowTools.Runtime.Utils
{
    public class HierarchyChangeDebounce
    {
        double _lastChangedTime;
        bool _pending;
        double _debounceDelay;

        public event Action hierarchyChanged;

        public HierarchyChangeDebounce(double debounceDelay = 0.2)
        {
            _debounceDelay = debounceDelay;
        }

        public void Enable()
        {
#if UNITY_EDITOR
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
            EditorApplication.update += OnUpdate;
#endif
        }

        public void Disable()
        {
#if UNITY_EDITOR
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.update -= OnUpdate;
            _pending = false;
#endif
        }

#if UNITY_EDITOR
        private void OnUpdate()
        {
            if (_pending && EditorApplication.timeSinceStartup - _lastChangedTime > _debounceDelay)
            {
                _pending = false;
                hierarchyChanged?.Invoke();
            }
        }

        private void OnHierarchyChanged()
        {
            _lastChangedTime = EditorApplication.timeSinceStartup;
            _pending = true;
        }


#endif
    }
}