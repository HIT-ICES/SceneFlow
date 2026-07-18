using System.Collections.Generic;
using UnityEngine;

namespace HighlightingSystem
{
    public static class HighlighterManager
    {
        private static int dirtyFrame = -1;

        private static readonly HashSet<Highlighter> highlighters = new();

        public static bool isDirty
        {
            get => dirtyFrame == Time.frameCount;
            private set => dirtyFrame = value ? Time.frameCount : -1;
        }

        // 
        public static void Add(Highlighter highlighter)
        {
            highlighters.Add(highlighter);
        }

        // 
        public static void Remove(Highlighter instance)
        {
            if (highlighters.Remove(instance) && instance.highlighted) isDirty = true;
        }

        // 
        public static HashSet<Highlighter>.Enumerator GetEnumerator()
        {
            return highlighters.GetEnumerator();
        }
    }
}