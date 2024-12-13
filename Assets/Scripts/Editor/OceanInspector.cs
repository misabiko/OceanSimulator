using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(Ocean))]
public class OceanInspector : Editor {
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
		var ocean = (Ocean)target;
		AddTexture(textureFoldout, "Displacement", ocean.displacement);
		AddTexture(textureFoldout, "dispFreqX", ocean.DispFreqX);
		AddTexture(textureFoldout, "dispFreqY", ocean.DispFreqY);
		AddTexture(textureFoldout, "dispFreqZ", ocean.DispFreqZ);
		AddTexture(textureFoldout, "dispSpatialX", ocean.DispSpatialX);
		AddTexture(textureFoldout, "dispSpatialY", ocean.DispSpatialY);
		AddTexture(textureFoldout, "dispSpatialZ", ocean.DispSpatialZ);
		AddTexture(textureFoldout, "normFreqY", ocean.NormFreqY);
		AddTexture(textureFoldout, "normSpatialY", ocean.NormSpatialY);
		AddTexture(textureFoldout, "Approximate Normals", ocean.ApproximateNormals);
		AddTexture(textureFoldout, "PingBuffer", ocean.PingBuffer);
		AddTexture(textureFoldout, "PongBuffer", ocean.PongBuffer);
		AddTexture(textureFoldout, "Noise", ocean.NoiseTexture);
		inspector.Add(textureFoldout);

		return inspector;
	}

	static void AddTexture(VisualElement parent, string name, Texture texture) {
		var foldout = new Foldout { text = name };
		foldout.Add(new Image { image = texture });
		parent.Add(foldout);
	}
}