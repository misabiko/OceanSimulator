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
		textureFoldout.schedule.Execute(() => {
			foreach (var e in textureFoldout.Children())
				if (e is Foldout foldout)
					foreach (var e2 in foldout.Children())
						if (e2 is Image image)
							image.MarkDirtyRepaint();
		}).Every(16);
		var ocean = (OceanMeshGenerator)target;
		AddTexture(textureFoldout, "Displacement", ocean.displacement);
		AddTexture(textureFoldout, "HX", ocean.HX);
		AddTexture(textureFoldout, "HY", ocean.HY);
		AddTexture(textureFoldout, "HZ", ocean.HZ);
		AddTexture(textureFoldout, "HX2", ocean.HX2);
		AddTexture(textureFoldout, "HY2", ocean.HY2);
		AddTexture(textureFoldout, "HZ2", ocean.HZ2);
		AddTexture(textureFoldout, "NY", ocean.NY);
		AddTexture(textureFoldout, "NY2", ocean.NY2);
		AddTexture(textureFoldout, "Approximate Normals", ocean.approximateNormals);
		AddTexture(textureFoldout, "PingBuffer", ocean.pingBuffer);
		AddTexture(textureFoldout, "PongBuffer", ocean.pongBuffer);
		AddTexture(textureFoldout, "Noise", ocean.noiseTexture);
		inspector.Add(textureFoldout);

		return inspector;
	}

	static void AddTexture(VisualElement parent, string name, Texture texture) {
		var foldout = new Foldout { text = name };
		foldout.Add(new Image { image = texture });
		parent.Add(foldout);
	}
}