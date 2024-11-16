using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(OceanMeshGenerator))]
public class OceanMeshGeneratorInspector : Editor {
	public VisualTreeAsset inspectorXML;
	public override VisualElement CreateInspectorGUI() {
		var inspector = new VisualElement();

		inspectorXML.CloneTree(inspector);

		InspectorElement.FillDefaultInspector(inspector.Q("DefaultInspector"), serializedObject, this);

		var textureFoldout = inspector.Q<Foldout>("TextureFoldout");
		textureFoldout.schedule.Execute((e) => {
			foreach (var texture in textureFoldout.Children())
				texture.MarkDirtyRepaint();
		}).Every(16);
		var ocean = (OceanMeshGenerator)target;
		textureFoldout.Add(new Image { image = ocean.displacement });
		textureFoldout.Add(new Image { image = ocean.HX });
		textureFoldout.Add(new Image { image = ocean.HY });
		textureFoldout.Add(new Image { image = ocean.HZ });
		textureFoldout.Add(new Image { image = ocean.HX2 });
		textureFoldout.Add(new Image { image = ocean.HY2 });
		textureFoldout.Add(new Image { image = ocean.HZ2 });
		textureFoldout.Add(new Image { image = ocean.NY });
		textureFoldout.Add(new Image { image = ocean.NY2 });
		textureFoldout.Add(new Image { image = ocean.approximateNormals });
		textureFoldout.Add(new Image { image = ocean.pingBuffer });
		textureFoldout.Add(new Image { image = ocean.pongBuffer });
		inspector.Add(textureFoldout);

		return inspector;
	}
}