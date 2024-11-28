using UnityEditor;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CustomEditor(typeof(GPUAnimationBaker))]
public class GpuAnimationBakerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector UI.
        DrawDefaultInspector();

        // Get a reference to the target script.
        GPUAnimationBaker exampleComponent = (GPUAnimationBaker) target;

        // Add a button to the Inspector.
        if (GUILayout.Button("Bake animation"))
        {
            exampleComponent.Bake();
        }
    }
}