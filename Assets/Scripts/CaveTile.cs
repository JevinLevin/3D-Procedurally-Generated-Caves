using System;
using PrimeTween;
using UnityEngine;

public class CaveTile : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private TileScriptableObject tile;
    [SerializeField] private Transform model;
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private ParticleSystem destroyParticles;

    [Header("Animation")] 
    public float mineScale = 1.2f;
    public float mineScaleInLength = 0.1f;
    public float mineScaleOutLength = 0.2f;
    public Ease mineScaleInEase = Ease.OutQuad;
    public Ease mineScaleOutEase = Ease.OutQuad;

    private int health;


    private void Start()
    {
        health = tile.maxHealth;
    }

    public void Setup(TileScriptableObject tile)
    {
        this.tile = tile;

        meshFilter.mesh = tile.mesh;
        meshRenderer.material = tile.material;
        
        var particleMain = destroyParticles.main;
        particleMain.startColor = tile.particleColors;
    }

    private Tween scaleTween;
    public void OnMine(int damage = 1)
    {
        
        health -= damage;
        if (health > 0)
        {
            if (scaleTween.isAlive)
                scaleTween.Complete();
            scaleTween = Tween.Scale(model, Vector3.one, Vector3.one * mineScale, mineScaleInLength, mineScaleInEase).OnComplete(() =>
            {
                if (scaleTween.isAlive)
                    scaleTween.Complete();
                scaleTween = Tween.Scale(model, Vector3.one * mineScale, Vector3.one, mineScaleOutLength, mineScaleOutEase);
            });
        }
        else
        {
            if (scaleTween.isAlive)
                scaleTween.Complete();
            
            destroyParticles.Play();
            destroyParticles.transform.SetParent(null);
            
            Score.OnAddScore?.Invoke(tile.value);
            Destroy(gameObject);
        }
    }
}
