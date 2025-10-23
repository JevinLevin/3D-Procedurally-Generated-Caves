using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMiner : MonoBehaviour
{
    [Header("Components")] 
    public Camera mainCamera;
    public Image explodeBar;
    
    [Header("Attributes")]
    public float mineRange = 5f;
    public LayerMask mineableLayer;
    public float explodeCooldown = 5;
    public int explodeDamage = 5;
    public float explodeRadius = 5;

    private bool active;
    private float explodeTimer = 0;
    
    void Update()
    {
        if(!active) return;
        
        if(Input.GetMouseButtonDown(0))
        {
            OnMine();
        }
        if(Input.GetMouseButtonUp(1) && explodeTimer <= 0)
        {
            OnExplode();
        }
        explodeTimer -= Time.deltaTime;
        explodeBar.fillAmount = explodeTimer / explodeCooldown;
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

    private void OnExplode()
    {
        explodeTimer = explodeCooldown;

        Vector3 explodeOrigin = mainCamera.transform.position + mainCamera.transform.forward * 1.5f;

        var targets = Physics.OverlapSphere(explodeOrigin, explodeRadius, mineableLayer);

        foreach(var target in targets)
        {
            if(target.TryGetComponent<CaveTile>(out var explodeTile))
            {
                explodeTile.OnMine(explodeDamage);
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
