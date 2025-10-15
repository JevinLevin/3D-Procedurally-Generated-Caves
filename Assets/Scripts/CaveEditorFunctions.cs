using EditorAttributes;
using UnityEngine;

public class CaveEditorFunctions : MonoBehaviour
{
    public CaveGenerator caveGenerator;
    public float stepTime;
    [Button]
    public void GenerateCave()
    {
        caveGenerator.stepTime = stepTime;
        caveGenerator.StartCoroutine(caveGenerator.FullGenerate());
    }
    [Button]
    public void ResetCave()
    {
        caveGenerator.ResetRoom();
        caveGenerator.ClearRoom();
    }
    
    
    
    
    [Button]
    public void StopGeneration()
    {
        caveGenerator.StopAllCoroutines();
    }
}
