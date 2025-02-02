﻿using System.Linq;
using Attributes;
using UnityEngine;
using UnityEditor;

namespace Editor
{
    [CustomPropertyDrawer(typeof(NamedListAttribute))]
    public class NamedListDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }
        
        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(rect, label, property);
            
            try 
            {
                var path = property.propertyPath;
                var pos = int.Parse(path.Split('[').LastOrDefault()?.TrimEnd(']') ?? string.Empty);
                EditorGUI.PropertyField(rect, property, new GUIContent(((NamedListAttribute) attribute).names[pos]), true);
            } 
            catch 
            {
                EditorGUI.PropertyField(rect, property, label, true);
            }
            
            EditorGUI.EndProperty();
        }
    }
}