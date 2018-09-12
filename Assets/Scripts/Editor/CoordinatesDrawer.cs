using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(HexagonCoordinates))]
public class CoordinatesDrawer : PropertyDrawer
{
	public override void OnGUI (Rect position, SerializedProperty property, GUIContent label) 
	{
		var coordinates = new HexagonCoordinates(property.FindPropertyRelative("x").intValue,
			property.FindPropertyRelative("z").intValue);
		
		position = EditorGUI.PrefixLabel(position, label);
		GUI.Label(position, coordinates.ToString());
	}
}
