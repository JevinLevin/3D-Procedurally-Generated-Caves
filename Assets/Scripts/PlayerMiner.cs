using UnityEngine;

public class PlayerMiner : MonoBehaviour
{
    [Header("Components")] 
    public Camera mainCamera;
    
    [Header("Attributes")]
    public float mineRange = 5f;
    public LayerMask mineableLayer;
    
    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            OnMine();
        }
    }

    private void OnMine()
    {
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        Physics.Raycast(ray, out var hit, mineRange, mineableLayer);
        if(hit.collider)
        {
            CaveTile caveTile = hit.collider.GetComponent<CaveTile>();
            if(caveTile)
            {
                caveTile.OnMine();
            }
        }
    }
}
