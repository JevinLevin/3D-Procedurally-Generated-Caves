using System;
using UnityEngine;

public class PlayerMiner : MonoBehaviour
{
    [Header("Components")] 
    public Camera mainCamera;
    
    [Header("Attributes")]
    public float mineRange = 5f;
    public LayerMask mineableLayer;

    private bool active;
    
    void Update()
    {
        if(!active) return;
        
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

    private void OnEnable()
    {
        GameManager.OnRoundEnd += DisablePlayer;
        GameManager.OnRoundStart += EnablePlayer;
    }
    private void OnDisable()
    {
        GameManager.OnRoundEnd -= DisablePlayer;
        GameManager.OnRoundStart -= EnablePlayer;
    }

    private void DisablePlayer()
    {
        active = false;
    }
    private void EnablePlayer()
    {
        active = true;
    }
}
